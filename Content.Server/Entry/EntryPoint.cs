using Content.Server.Acz;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Afk;
using Content.Server.Chat.Managers;
using Content.Server.Connection;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.GhostKick;
using Content.Server.GuideGenerator;
using Content.Server.Info;
using Content.Server.IoC;
using Content.Server.Maps;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.ServerInfo;
using Content.Server.ServerUpdates;
using Content.Server.Voting.Managers;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Kitchen;
using Robust.Server;
using Robust.Server.Bql;
using Robust.Shared.Configuration;
using Robust.Server.ServerStatus;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Server.Station.Systems;
using Content.Shared.Localizations;

namespace Content.Server.Entry
{
    public sealed class EntryPoint : GameServer
    {
        private const string ConfigPresetsDir = "/ConfigPresets/";
        private const string ConfigPresetsDirBuild = $"{ConfigPresetsDir}Build/";

        private EuiManager _euiManager = default!;
        private IVoteManager _voteManager = default!;
        private ServerUpdateManager _updateManager = default!;
        private PlayTimeTrackingManager? _playTimeTracking;
        private IEntitySystemManager? _sysMan;

        /// <inheritdoc />
        public override void Init()
        {
            base.Init();

            var cfg = IoCManager.Resolve<IConfigurationManager>();
            var res = IoCManager.Resolve<IResourceManager>();
            var logManager = IoCManager.Resolve<ILogManager>();

            LoadConfigPresets(cfg, res, logManager.GetSawmill("configpreset"));

            var aczProvider = new ContentMagicAczProvider(IoCManager.Resolve<IDependencyCollection>());
            IoCManager.Resolve<IStatusHost>().SetMagicAczProvider(aczProvider);

            var factory = IoCManager.Resolve<IComponentFactory>();
            var prototypes = IoCManager.Resolve<IPrototypeManager>();

            factory.DoAutoRegistrations();
            factory.IgnoreMissingComponents("Visuals");

            foreach (var ignoreName in IgnoredComponents.List)
            {
                factory.RegisterIgnore(ignoreName);
            }

            prototypes.RegisterIgnore("parallax");
            prototypes.RegisterIgnore("guideEntry");

            ServerContentIoC.Register();

            foreach (var callback in TestingCallbacks)
            {
                var cast = (ServerModuleTestingCallbacks) callback;
                cast.ServerBeforeIoC?.Invoke();
            }

            IoCManager.BuildGraph();
            factory.GenerateNetIds();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            var dest = configManager.GetCVar(CCVars.DestinationFile);
            IoCManager.Resolve<ContentLocalizationManager>().Initialize();
            if (string.IsNullOrEmpty(dest)) //hacky but it keeps load times for the generator down.
            {
                _euiManager = IoCManager.Resolve<EuiManager>();
                _voteManager = IoCManager.Resolve<IVoteManager>();
                _updateManager = IoCManager.Resolve<ServerUpdateManager>();
                _playTimeTracking = IoCManager.Resolve<PlayTimeTrackingManager>();
                _sysMan = IoCManager.Resolve<IEntitySystemManager>();

                logManager.GetSawmill("Storage").Level = LogLevel.Info;
                logManager.GetSawmill("db.ef").Level = LogLevel.Info;

                IoCManager.Resolve<IAdminLogManager>().Initialize();
                IoCManager.Resolve<IConnectionManager>().Initialize();
                IoCManager.Resolve<IServerDbManager>().Init();
                IoCManager.Resolve<IServerPreferencesManager>().Init();
                IoCManager.Resolve<INodeGroupFactory>().Initialize();
                IoCManager.Resolve<IGamePrototypeLoadManager>().Initialize();
                IoCManager.Resolve<NetworkResourceManager>().Initialize();
                IoCManager.Resolve<GhostKickManager>().Initialize();
                IoCManager.Resolve<ServerInfoManager>().Initialize();

                _voteManager.Initialize();
                _updateManager.Initialize();
                _playTimeTracking.Initialize();
            }
        }

        public override void PostInit()
        {
            base.PostInit();

            IoCManager.Resolve<IChatSanitizationManager>().Initialize();
            IoCManager.Resolve<IChatManager>().Initialize();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            var resourceManager = IoCManager.Resolve<IResourceManager>();
            var dest = configManager.GetCVar(CCVars.DestinationFile);
            if (!string.IsNullOrEmpty(dest))
            {
                var resPath = new ResourcePath(dest).ToRootedPath();
                var file = resourceManager.UserData.OpenWriteText(resPath.WithName("chem_" + dest));
                ChemistryJsonGenerator.PublishJson(file);
                file.Flush();
                file = resourceManager.UserData.OpenWriteText(resPath.WithName("react_" + dest));
                ReactionJsonGenerator.PublishJson(file);
                file.Flush();
                IoCManager.Resolve<IBaseServer>().Shutdown("Data generation done");
            }
            else
            {
                IoCManager.Resolve<RecipeManager>().Initialize();
                IoCManager.Resolve<IAdminManager>().Initialize();
                IoCManager.Resolve<IAfkManager>().Initialize();
                IoCManager.Resolve<RulesManager>().Initialize();
                _euiManager.Initialize();

                IoCManager.Resolve<IGameMapManager>().Initialize();
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GameTicker>().PostInitialize();
                IoCManager.Resolve<IBqlQueryManager>().DoAutoRegistrations();
                IoCManager.Resolve<RoleBanManager>().Initialize();
            }
        }

        public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
        {
            base.Update(level, frameEventArgs);

            switch (level)
            {
                case ModUpdateLevel.PostEngine:
                {
                    _euiManager.SendUpdates();
                    _voteManager.Update();
                    break;
                }

                case ModUpdateLevel.FramePostEngine:
                    _updateManager.Update();
                    _playTimeTracking?.Update();
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _playTimeTracking?.Shutdown();
            _sysMan?.GetEntitySystemOrNull<StationSystem>()?.OnServerDispose();
        }

        private static void LoadConfigPresets(IConfigurationManager cfg, IResourceManager res, ISawmill sawmill)
        {
            LoadBuildConfigPresets(cfg, res, sawmill);

            var presets = cfg.GetCVar(CCVars.ConfigPresets);
            if (presets == "")
                return;

            foreach (var preset in presets.Split(','))
            {
                var path = $"{ConfigPresetsDir}{preset}.toml";
                if (!res.TryContentFileRead(path, out var file))
                {
                    sawmill.Error("Unable to load config preset {Preset}!", path);
                    continue;
                }

                cfg.LoadDefaultsFromTomlStream(file);
                sawmill.Info("Loaded config preset: {Preset}", path);
            }
        }

        private static void LoadBuildConfigPresets(IConfigurationManager cfg, IResourceManager res, ISawmill sawmill)
        {
#if !FULL_RELEASE
            Load(CCVars.ConfigPresetDevelopment, "development");
#endif
#if DEBUG
            Load(CCVars.ConfigPresetDebug, "debug");
#endif

            void Load(CVarDef<bool> cVar, string name)
            {
                var path = $"{ConfigPresetsDirBuild}{name}.toml";
                if (cfg.GetCVar(cVar) && res.TryContentFileRead(path, out var file))
                {
                    cfg.LoadDefaultsFromTomlStream(file);
                    sawmill.Info("Loaded config preset: {Preset}", path);
                }
            }
        }
    }
}
