using SpaceOS.Modules.Joinery.Domain.Rules;

namespace SpaceOS.Modules.Joinery.Infrastructure.Seeding;


/// <summary>
/// Static seed data for the Doorstar reference tables: global constants, door-type rules, and process task templates.
/// </summary>
public static class DoorstarSeedData
{
    /// <summary>Global engineering constants shared by all calculation services.</summary>
    public static IReadOnlyList<GlobalConstant> Constants { get; } =
    [
        new GlobalConstant { Key = "CuttingOversize",  Value = 1m    },
        new GlobalConstant { Key = "CladdingOverhang", Value = 0.2m  },
        new GlobalConstant { Key = "MatyiWidth",       Value = 4.6m  },
    ];

    /// <summary>Per-door-type rules that drive BKM panel sizing.</summary>
    public static IReadOnlyList<DoorTypeRule> DoorTypeRules { get; } =
    [
        new DoorTypeRule { DoorType = "Disztok",    AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "FAF_T",      AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "FAF_TN",     AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "Falsikban",  AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "Falcos",     AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "Tokba",      AjtólapCount = 2, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "Sikban",     AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "Pivot",      AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        // Extended set (≥15 total)
        new DoorTypeRule { DoorType = "Kétszárnyú", AjtólapCount = 2, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "Tűzálló",   AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "Üveges",    AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "Csúszó",    AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "Redőnyes",  AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "Akusztikus", AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
        new DoorTypeRule { DoorType = "Forgóajtó", AjtólapCount = 4, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
    ];

    /// <summary>Per-door-type part dimension rules driving cutting calculations.</summary>
    public static IReadOnlyList<PartDimensionRule> PartDimensionRules { get; } =
    [
        new PartDimensionRule { DoorType = "Disztok",   ComponentName = "Frame-Oldal",    ComponentType = "Frame",     Quantity = 2, WidthBase = 0m,    WidthMultiplierFactor = 1m, LengthBase = 0m,   LengthMultiplierFactor = 1m },
        new PartDimensionRule { DoorType = "Disztok",   ComponentName = "Frame-Felső",    ComponentType = "Frame",     Quantity = 1, WidthBase = 0m,    WidthMultiplierFactor = 1m, LengthBase = 0m,   LengthMultiplierFactor = 0m },
        new PartDimensionRule { DoorType = "Falcos",    ComponentName = "Insert-Panel",   ComponentType = "Insert",    Quantity = 1, WidthBase = 2m,    WidthMultiplierFactor = 1m, LengthBase = 2m,   LengthMultiplierFactor = 1m },
        new PartDimensionRule { DoorType = "Falcos",    ComponentName = "Clad-Oldal",     ComponentType = "Clad",      Quantity = 2, WidthBase = 0m,    WidthMultiplierFactor = 1m, LengthBase = 4m,   LengthMultiplierFactor = 1m },
        new PartDimensionRule { DoorType = "FAF_T",     ComponentName = "FrameCore-Alap", ComponentType = "FrameCore", Quantity = 1, WidthBase = 0m,    WidthMultiplierFactor = 1m, LengthBase = 0m,   LengthMultiplierFactor = 1m },
        new PartDimensionRule { DoorType = "FAF_TN",    ComponentName = "Blende-Csík",    ComponentType = "Blende",    Quantity = 4, WidthBase = 40m,   WidthMultiplierFactor = 0m, LengthBase = 0m,   LengthMultiplierFactor = 1m },
        new PartDimensionRule { DoorType = "Falsikban", ComponentName = "Coating-Lap",    ComponentType = "Coating",   Quantity = 1, WidthBase = 0m,    WidthMultiplierFactor = 1m, LengthBase = 0m,   LengthMultiplierFactor = 1m },
    ];

    /// <summary>Standard process task templates for the Gyártás workflow phases.</summary>
    public static IReadOnlyList<ProcessTaskTemplate> ProcessTasks { get; } =
    [
        // GyI-E: Előkészítés (Preparation)
        new ProcessTaskTemplate { TaskId = "GyI-E.01", ShortName = "Előkészítés",          Description = "Gyártás I. előkészítés",                Department = "Gyártás",     UnitTimeSec = 300,  Headcount = 1, ParentTaskId = null       },
        new ProcessTaskTemplate { TaskId = "GyI-E.02", ShortName = "Anyagkiadás",           Description = "Anyag kiadása a raktárból",             Department = "Raktár",      UnitTimeSec = 180,  Headcount = 1, ParentTaskId = "GyI-E.01" },
        new ProcessTaskTemplate { TaskId = "GyI-E.03", ShortName = "Szabás",                Description = "Lemezek/lapok szabása",                 Department = "Gyártás",     UnitTimeSec = 600,  Headcount = 1, ParentTaskId = "GyI-E.01" },
        // GyI-T: Tokgyártás (Frame manufacturing)
        new ProcessTaskTemplate { TaskId = "GyI-T.01", ShortName = "Tokgyártás",            Description = "Ajtótok összeszerelésa",                Department = "Gyártás",     UnitTimeSec = 900,  Headcount = 2, ParentTaskId = null       },
        new ProcessTaskTemplate { TaskId = "GyI-T.02", ShortName = "Tokfelület-kezelés",    Description = "Tok festése / bevonat",                 Department = "Felület",     UnitTimeSec = 1200, Headcount = 1, ParentTaskId = "GyI-T.01" },
        new ProcessTaskTemplate { TaskId = "GyI-T.03", ShortName = "Tokminőség-ellenőrzés", Description = "Tok QC",                                Department = "Minőség",     UnitTimeSec = 300,  Headcount = 1, ParentTaskId = "GyI-T.01" },
        new ProcessTaskTemplate { TaskId = "GyI-T.04", ShortName = "Tokraktár",             Description = "Tok raktározás / mozgatás",             Department = "Raktár",      UnitTimeSec = 180,  Headcount = 1, ParentTaskId = "GyI-T.01" },
        // GyI-L: Lapgyártás (Panel manufacturing)
        new ProcessTaskTemplate { TaskId = "GyI-L.01", ShortName = "Lapvágás",              Description = "Panel alapméretre vágása",              Department = "Gyártás",     UnitTimeSec = 480,  Headcount = 1, ParentTaskId = null       },
        new ProcessTaskTemplate { TaskId = "GyI-L.02", ShortName = "Élzárás",               Description = "Panel éleinek lezárása élzáró géppel",  Department = "Gyártás",     UnitTimeSec = 360,  Headcount = 1, ParentTaskId = "GyI-L.01" },
        new ProcessTaskTemplate { TaskId = "GyI-L.03", ShortName = "Marás",                 Description = "Maratási profil kialakítása CNC-vel",   Department = "Gyártás",     UnitTimeSec = 720,  Headcount = 1, ParentTaskId = "GyI-L.01" },
        new ProcessTaskTemplate { TaskId = "GyI-L.04", ShortName = "Furás",                 Description = "Zsanér és zár furatok készítése",        Department = "Gyártás",     UnitTimeSec = 300,  Headcount = 1, ParentTaskId = "GyI-L.01" },
        new ProcessTaskTemplate { TaskId = "GyI-L.05", ShortName = "Csiszolás",             Description = "Felület csiszolása festés előtt",        Department = "Gyártás",     UnitTimeSec = 420,  Headcount = 1, ParentTaskId = "GyI-L.01" },
        new ProcessTaskTemplate { TaskId = "GyI-L.06", ShortName = "Lapminőség-ellenőrzés", Description = "Panel QC mérőasztalnál",                Department = "Minőség",     UnitTimeSec = 240,  Headcount = 1, ParentTaskId = "GyI-L.01" },
        // GyI-V: Vasalás (Hardware fitting)
        new ProcessTaskTemplate { TaskId = "GyI-V.01", ShortName = "Vasalat-kiadás",        Description = "Vasalat szett kiadása a raktárból",     Department = "Raktár",      UnitTimeSec = 120,  Headcount = 1, ParentTaskId = null       },
        new ProcessTaskTemplate { TaskId = "GyI-V.02", ShortName = "Zsanér-beépítés",       Description = "Zsanérok beemelése és rögzítése",       Department = "Gyártás",     UnitTimeSec = 600,  Headcount = 1, ParentTaskId = "GyI-V.01" },
        new ProcessTaskTemplate { TaskId = "GyI-V.03", ShortName = "Zárszerelés",           Description = "Zárszerkezet beépítése és beállítása",  Department = "Gyártás",     UnitTimeSec = 480,  Headcount = 1, ParentTaskId = "GyI-V.01" },
        new ProcessTaskTemplate { TaskId = "GyI-V.04", ShortName = "Kilincs-szerelés",      Description = "Kilincs garnitúra felszerelése",        Department = "Gyártás",     UnitTimeSec = 300,  Headcount = 1, ParentTaskId = "GyI-V.01" },
        new ProcessTaskTemplate { TaskId = "GyI-V.05", ShortName = "Vasalat-ellenőrzés",    Description = "Vasalati elemek funkció-ellenőrzése",   Department = "Minőség",     UnitTimeSec = 240,  Headcount = 1, ParentTaskId = "GyI-V.01" },
        // GyII-A: Összeszereléss (Assembly)
        new ProcessTaskTemplate { TaskId = "GyII-A.01", ShortName = "Szereléss-előkészítés", Description = "Összeszereléss munkaállomás előkészítése", Department = "Gyártás",  UnitTimeSec = 300,  Headcount = 1, ParentTaskId = null        },
        new ProcessTaskTemplate { TaskId = "GyII-A.02", ShortName = "Tok-lap illesztés",    Description = "Tok és ajtólap első illesztése",        Department = "Gyártás",     UnitTimeSec = 900,  Headcount = 2, ParentTaskId = "GyII-A.01" },
        new ProcessTaskTemplate { TaskId = "GyII-A.03", ShortName = "Tömítés",              Description = "Tömítőszalag felhelyezése a tokra",     Department = "Gyártás",     UnitTimeSec = 480,  Headcount = 1, ParentTaskId = "GyII-A.01" },
        new ProcessTaskTemplate { TaskId = "GyII-A.04", ShortName = "Keret-rögzítés",       Description = "Keret elemek végleges rögzítése",       Department = "Gyártás",     UnitTimeSec = 600,  Headcount = 2, ParentTaskId = "GyII-A.01" },
        new ProcessTaskTemplate { TaskId = "GyII-A.05", ShortName = "Szerelés-ellenőrzés",  Description = "Összeszereléss utáni funkció-teszt",    Department = "Minőség",     UnitTimeSec = 360,  Headcount = 1, ParentTaskId = "GyII-A.01" },
        // GyII-F: Felületkezelés (Surface treatment)
        new ProcessTaskTemplate { TaskId = "GyII-F.01", ShortName = "Alapozás",             Description = "Felület alapozó réteg felvitele",       Department = "Felület",     UnitTimeSec = 600,  Headcount = 1, ParentTaskId = null        },
        new ProcessTaskTemplate { TaskId = "GyII-F.02", ShortName = "Festés",               Description = "Fedőfesték felvitele pisztolyos géppel", Department = "Felület",    UnitTimeSec = 900,  Headcount = 1, ParentTaskId = "GyII-F.01" },
        new ProcessTaskTemplate { TaskId = "GyII-F.03", ShortName = "Fóliázás",             Description = "Dekor fólia kasírozása vákuumpréssel",  Department = "Felület",     UnitTimeSec = 1200, Headcount = 2, ParentTaskId = "GyII-F.01" },
        new ProcessTaskTemplate { TaskId = "GyII-F.04", ShortName = "Száradás",             Description = "Szárítókamrás kezelés min. 120 perc",   Department = "Felület",     UnitTimeSec = 7200, Headcount = 0, ParentTaskId = "GyII-F.01" },
        new ProcessTaskTemplate { TaskId = "GyII-F.05", ShortName = "Felület-ellenőrzés",   Description = "Felület vizuális és tapintás QC",       Department = "Minőség",     UnitTimeSec = 300,  Headcount = 1, ParentTaskId = "GyII-F.01" },
        // GyV-B: Beépítés (Installation)
        new ProcessTaskTemplate { TaskId = "GyV-B.01", ShortName = "Beépítés",              Description = "Ajtó helyszíni beépítése",              Department = "Szerelés",    UnitTimeSec = 2400, Headcount = 2, ParentTaskId = null       },
        new ProcessTaskTemplate { TaskId = "GyV-B.02", ShortName = "Beépítés-ellenőrzés",  Description = "Beépítés utáni funkcionális teszt",     Department = "Minőség",     UnitTimeSec = 600,  Headcount = 1, ParentTaskId = "GyV-B.01" },
        new ProcessTaskTemplate { TaskId = "GyV-B.03", ShortName = "Átadás",               Description = "Ügyfélatadás és dokumentáció",          Department = "Értékesítés", UnitTimeSec = 600,  Headcount = 1, ParentTaskId = "GyV-B.01" },
        // GyV-C: Csomagolás (Packaging)
        new ProcessTaskTemplate { TaskId = "GyV-C.01", ShortName = "Csomagolás",            Description = "Ajtó karton és habszivacs csomagolása",  Department = "Logisztika",  UnitTimeSec = 480,  Headcount = 1, ParentTaskId = null       },
        new ProcessTaskTemplate { TaskId = "GyV-C.02", ShortName = "Védőfólia",             Description = "Felületre védőfólia ragasztása",         Department = "Logisztika",  UnitTimeSec = 300,  Headcount = 1, ParentTaskId = "GyV-C.01" },
        new ProcessTaskTemplate { TaskId = "GyV-C.03", ShortName = "Jelölés",               Description = "Szállítási és projekt-jelölés felhelyezése", Department = "Logisztika", UnitTimeSec = 120, Headcount = 1, ParentTaskId = "GyV-C.01" },
        new ProcessTaskTemplate { TaskId = "GyV-C.04", ShortName = "Palettázás",            Description = "Csomagolt ajtók palettára rakása",      Department = "Logisztika",  UnitTimeSec = 360,  Headcount = 2, ParentTaskId = "GyV-C.01" },
        new ProcessTaskTemplate { TaskId = "GyV-C.05", ShortName = "Szállítás-előkészítés", Description = "Fuvarlevél és szállítási papírok",      Department = "Logisztika",  UnitTimeSec = 240,  Headcount = 1, ParentTaskId = "GyV-C.01" },
        // GyR-J: Javítás (Repair)
        new ProcessTaskTemplate { TaskId = "GyR-J.01", ShortName = "Hibajelentés",          Description = "Reklamáció vagy gyártási hiba rögzítése", Department = "Minőség",    UnitTimeSec = 180,  Headcount = 1, ParentTaskId = null       },
        new ProcessTaskTemplate { TaskId = "GyR-J.02", ShortName = "Diagnosztika",          Description = "Hiba okának feltárása és dokumentálása", Department = "Minőség",    UnitTimeSec = 600,  Headcount = 1, ParentTaskId = "GyR-J.01" },
        new ProcessTaskTemplate { TaskId = "GyR-J.03", ShortName = "Javítás",               Description = "Hibás komponens javítása vagy cseréje",  Department = "Gyártás",    UnitTimeSec = 1800, Headcount = 1, ParentTaskId = "GyR-J.01" },
        new ProcessTaskTemplate { TaskId = "GyR-J.04", ShortName = "Újra-ellenőrzés",       Description = "Javított egység ismételt QC átvilágítás", Department = "Minőség",   UnitTimeSec = 300,  Headcount = 1, ParentTaskId = "GyR-J.01" },
        new ProcessTaskTemplate { TaskId = "GyR-J.05", ShortName = "Javítás-lezárás",       Description = "Javítási eset lezárása rendszerben",    Department = "Minőség",     UnitTimeSec = 120,  Headcount = 1, ParentTaskId = "GyR-J.01" },
    ];
}
