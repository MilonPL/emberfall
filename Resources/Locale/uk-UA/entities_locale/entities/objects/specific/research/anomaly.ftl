ent-AnomalyScanner = anomaly scanner
    .desc = A hand-held scanner built to collect information on various anomalous objects.

ent-AnomalyLocatorUnpowered = anomaly locator
    .desc = A device designed to aid in the locating of anomalies. Did you check the gas miners?
    .suffix = Unpowered

ent-AnomalyLocator = { ent-[ AnomalyLocatorUnpowered, PowerCellSlotSmallItem ] }
    .desc = { ent-[ AnomalyLocatorUnpowered, PowerCellSlotSmallItem ].desc }
    .suffix = Powered

ent-AnomalyLocatorEmpty = { ent-AnomalyLocator }
    .desc = { ent-AnomalyLocator.desc }
    .suffix = Empty

ent-AnomalyLocatorWideUnpowered = wide-spectrum anomaly locator
    .desc = A device that looks for anomalies from an extended distance, but has no way to determine the distance to them.
    .suffix = Unpowered

ent-AnomalyLocatorWide = { ent-[ AnomalyLocatorWideUnpowered, PowerCellSlotSmallItem ] }
    .desc = { ent-[ AnomalyLocatorWideUnpowered, PowerCellSlotSmallItem ].desc }
    .suffix = Powered

ent-AnomalyLocatorWideEmpty = { ent-AnomalyLocatorWide }
    .desc = { ent-AnomalyLocatorWide.desc }
    .suffix = Empty

