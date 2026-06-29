
// AUTO-SYNCED from Midnight Metro SaveModels.cs
namespace MidnightMetroEditor.Models
{
    public class MetroSaveFile
    {
        public int version = 3;
        public string savedUtc;
        public MetroSaveSession session = new();
        public MetroSaveGrid grid = new();
        public MetroSaveResidents residents = new();
        /// <summary>Archived deceased — lineage / widowed spouse lookups (v22+).</summary>
        public MetroSaveResidents deceased = new();
        /// <summary>Sparse underground metro cells (v18+).</summary>
        public MetroSaveSubway subway = new();
        /// <summary>News feed archive (v31+).</summary>
        public MetroSaveNews news = new();
        /// <summary>Named metro corridors (v31+).</summary>
        public MetroSaveMetroLines metroLines = new();
        /// <summary>Station rental bikes + citizen vehicle state (v32+).</summary>
        public MetroSaveMobility mobility = new();
        /// <summary>Mayor / council election calendar (v38+).</summary>
        public MetroSaveElections elections = new();
        /// <summary>Prosecution cases (v43+).</summary>
        public MetroSaveJustice justice = new();
    }
    public class MetroSaveMobility
    {
        public int fleetSize;
        public int bikesInTransit;
        public int[]? rentalStationX;
        public int[]? rentalStationY;
        public int[]? rentalStationBikes;
    }
    public class MetroSaveSubway
    {
        public int[]? x;
        public int[]? y;
        public int[]? kind;
        /// <summary>Straight tunnel link endpoints (v19+).</summary>
        public int[]? linkAx;
        public int[]? linkAy;
        public int[]? linkBx;
        public int[]? linkBy;
        /// <summary>Multi-depth stations and link metadata (v20+).</summary>
        public int[]? stationDepthFlat;
        public int[]? linkADepth;
        public int[]? linkBDepth;
        public int[]? linkKind;
    }
    public class MetroSaveSession
{
    public const int DefaultStartingTreasury = 500_000;
        public int day = 1;
        public string cityName = "Midnight Metro";
        public int randomSeed = 12345;
        public int treasury = MetroSaveSession.DefaultStartingTreasury;
        /// <summary>Local calendar date when the city was founded; sim day 1.</summary>
        public int creationYear;
        public int creationMonth;
        public int creationDay;
        /// <summary>Auto growth toggles (v10+).</summary>
        public int autoZoneFromDemand = 1;
        public int autoGrowResidential = 1;
        public int autoGrowCommercial = 1;
        public int autoGrowMixed = 1;
        public int autoGrowOffice = 1;
        public int autoGrowIndustrial = 1;
        /// <summary>Auto-place utility/service buildings when demand prompts fire (v28+).</summary>
        public int autoPlaceUtilities = 1;
        /// <summary>Auto-place police academy/station when crime prompts fire (v30+).</summary>
        public int autoPlacePolice = 1;
        public int showPurchasableBlocks = 1;
        /// <summary>1 when written by auto-save (v11+).</summary>
        public int isAutoSave;
        /// <summary>World coordinates for regional ethnicity (v16+).</summary>
        public float latitude;
        public float longitude;
        /// <summary>Linked crime + police bundle tier 0–4 (v29+).</summary>
        public int crimeProgressionTier;
        /// <summary>Sim day when B1 street bundle was declined; 0 = never (v29+).</summary>
        public int streetCrimeBundleDeclinedDay;
        /// <summary>1 after save editor / repair cleared metro — planner expands for a few days (v39+).</summary>
        public int metroNetworkResetPending;
        /// <summary>Highway link cell for regional state prison (v42+); -1 when unset.</summary>
        public int regionalPrisonLinkX = -1;
        public int regionalPrisonLinkY = -1;
        /// <summary>Federal task-force contract active (v42+).</summary>
        public int federalPoliceContractActive;
        public int federalPoliceContractSignedDay;
        public int federalPoliceContractDaysRemaining;
    }
    public class MetroSaveGrid
    {
        public int width;
        public int height;
        public int[]? type;
        public int[]? population;
        public int[]? wealth;
        public int[]? ownerRosterId;
        /// <summary>1 = unlocked land block, 0 = locked (v5+).</summary>
        public int[]? chunkUnlocked;
        /// <summary>Per-cell zone enum value (v6+).</summary>
        public int[]? zone;
        /// <summary>Lot footprint + floors (v7+).</summary>
        public int[]? lotWidth;
        public int[]? lotHeight;
        public int[]? lotAnchorX;
        public int[]? lotAnchorY;
        public int[]? maxFloors;
        public int[]? builtFloors;
        public string[]? buildingItemId;
        /// <summary>Rooftop workshop attachments (v9+).</summary>
        public string[]? rooftopItemId;
        /// <summary>Street class, lanes, lights (v8+).</summary>
        public int[]? streetRoadClass;
        public int[]? streetRoadLanes;
        public int[]? streetLights;
        /// <summary>0–1000 scaled pollution + density tier (v12+).</summary>
        public int[]? groundPollution;
        public int[]? airPollution;
        public int[]? noisePollution;
        public int[]? densityTier;
        /// <summary>Construction state (v13+).</summary>
        public int[]? constructionCompleteDay;
        public int[]? constructionKind;
        public int[]? pendingType;
        public int[]? pendingMaxFloors;
        public int[]? pendingBuiltFloors;
        public int[]? pendingPopulation;
        public int[]? pendingWealth;
        public int[]? pendingDensityTier;
        public int[]? pendingLotWidth;
        public int[]? pendingLotHeight;
        public string[]? pendingBuildingItemId;
        public int[]? mixedUpperUse;
        /// <summary>Construction animation start snapshot (v15+).</summary>
        public int[]? constructionStartDay;
        public int[]? constructionStartLotWidth;
        public int[]? constructionStartLotHeight;
        public int[]? constructionStartBuiltFloors;
        /// <summary>Workplace staffing and supply chain (v26+).</summary>
        public int[]? jobSlotTarget;
        public int[]? vacancyStreakDays;
        public int[]? supplyFulfillment;
        public int[]? customerFulfillment;
        /// <summary>Uncollected refuse 0–1000 scaled (v28+).</summary>
        public int[]? garbageBacklog;
        /// <summary>Backed-up sewer 0–500 scaled (v28+).</summary>
        public int[]? sewageBacklog;
        /// <summary>Garbage truck visit strength 0–255 (v28+).</summary>
        public int[]? garbageCollectionLevel;
        /// <summary>Mail van visit strength 0–255 (v28+).</summary>
        public int[]? postalDeliveryLevel;
        /// <summary>Street utility carrier flags per cell (v28+).</summary>
        public int[]? streetUtilityNetworks;
    }
    public class MetroSaveResidents
    {
        public int[]? rosterId;
        public int[]? givenNameId;
        public int[]? familyNameId;
        public int[]? appearanceSex;
        public int[]? appearanceEthnicity;
        public int[]? homeX;
        public int[]? homeY;
        public int[]? workplaceX;
        public int[]? workplaceY;
        public int[]? job;
        public int[]? traitIntelligence;
        public int[]? traitHealth;
        public int[]? traitAggression;
        /// <summary>Founder DNA hex (v16+).</summary>
        public string[]? geneticDna;
        /// <summary>Age in years (v17+).</summary>
        public float[]? ageYears;
        /// <summary>1 = retired from workforce (v17+).</summary>
        public int[]? isRetired;
        /// <summary>Family & education (v21+).</summary>
        public int[]? spouseRosterId;
        public int[]? partnerRosterId;
        public int[]? partnerBondDays;
        public int[]? parent1RosterId;
        public int[]? parent2RosterId;
        public int[]? isPregnant;
        public int[]? pregnancyDaysRemaining;
        public int[]? pregnancyFatherRosterId;
        public int[]? pregnancyOutOfWedlock;
        public int[]? pregnancyFetusCount;
        public int[]? pregnancyContext;
        public int[]? reproductiveCycleDay;
        public int[]? educationLevel;
        public float[]? educationYears;
        public int[]? religionId;
        /// <summary>First intercourse age (v23+, adult-content saves only).</summary>
        public float[]? virginityLostAgeYears;
        /// <summary>School enrollment & family schedule (v24+).</summary>
        public int[]? schoolX;
        public int[]? schoolY;
        public int[]? exSpouseRosterId;
        public int[]? custodyDaysMask;
        public int[]? workAbsencesThisWeek;
        public int[]? employmentWarnings;
        /// <summary>School enrollment, daycare, unemployment (v25+).</summary>
        public int[]? unemployedDays;
        public int[]? daycareX;
        public int[]? daycareY;
        public int[]? attendanceWeekIndex;
        /// <summary>Cause of death for archived residents (v27+).</summary>
        public int[]? deathCause;
        /// <summary><see cref="CitizenSchoolPlacement"/> per resident (v29+).</summary>
        public int[]? schoolPlacement;
        /// <summary>Stay-home parent roster id when placement is StayHomeParent (v29+).</summary>
        public int[]? stayHomeParentRosterId;
        /// <summary>Personal mobility assets (v32+).</summary>
        public int[]? ownsCar;
        public int[]? ownsPersonalBike;
        public int[]? carStolen;
        public int[]? bikeStolen;
        public int[]? carParkedX;
        public int[]? carParkedY;
        public int[]? personalBikeParkedX;
        public int[]? personalBikeParkedY;
        /// <summary><see cref="CitizenBirthOrigin"/> per resident (v36+).</summary>
        public int[]? birthOrigin;
        /// <summary><see cref="CitizenConceptionKind"/> per resident (v40+).</summary>
        public int[]? conceptionKind;
        /// <summary>Crime dossier fields (v37+).</summary>
        public int[]? criminalRecord;
        public int[]? boloActive;
        public int[]? boloIssuedDay;
        public int[]? boloCrimeKind;
        public int[]? boloLotX;
        public int[]? boloLotY;
        public int[]? boloNewsArticleId;
        public int[]? warrantActive;
        public int[]? warrantIssuedDay;
        public int[]? warrantCrimeKind;
        public int[]? warrantLotX;
        public int[]? warrantLotY;
        public int[]? stationDetained;
        public int[]? stationDetainReleaseDay;
        public float[]? stationDetainReleaseHour;
        public int[]? stationDetainLotX;
        public int[]? stationDetainLotY;
        public int[]? stationDetainChargeKind;
        /// <summary>Adult vs juvenile conviction counters (v38+).</summary>
        public int[]? adultConvictionCount;
        public int[]? juvenileConvictionCount;
        /// <summary>Jail sentence end day + active case link (v43+).</summary>
        public int[]? incarceratedUntilDay;
        public int[]? activeJusticeCaseId;
    }
    public class MetroSaveJustice
    {
        public int nextCaseId = 1;
        public MetroSaveJusticeCaseRow[] cases;
    }
    public class MetroSaveJusticeCaseRow
    {
        public int caseId;
        public int offenderRosterId;
        public int victimRosterId;
        public int crimeKind;
        public int incidentX;
        public int incidentY;
        public int stage;
        public int evidenceScore;
        public int openedDay;
        public int stageDay;
        public int trialDay;
        public int incarceratedUntilDay;
        public int fineAmount;
        public int assignedJudgeRosterId;
        public int verdict;
        public int sentenceDays;
        public int mistrialCount;
    }
    public class MetroSaveMayorHonorRow
    {
        public int rosterId;
        public int termStartDay;
        public int termEndDay;
        public int consecutiveTerms;
        /// <summary>1 when seated by interim appointment, not election.</summary>
        public int interim;
    }
    public class MetroSaveElections
    {
        public int cycleAnchorDay;
        public int nextMayorElectionDay;
        public int nextCouncilElectionDay;
        public int mayorTermStartDay;
        public int mayorConsecutiveTerms;
        public int mayorRosterId;
        public int mayorInterim;
        public int activeRace;
        public int campaignStartDay;
        public int electionDay;
        public int specialElection;
        public int[]? mayorCandidates;
        public int[]? mayorVotePermille;
        public int[]? councilSlateMembers;
        public int[]? councilSlateVotePermille;
        public int councilSlateSize;
        public MetroSaveMayorHonorRow[] mayorHonor;
    }
    public class MetroSaveNewsArticleRow
    {
        public int id;
        public int day;
        public float hour;
        public int category;
        public string outlet;
        public string headline;
        public string subhead;
        public string body;
        public int lotX = -1;
        public int lotY;
        public int subjectRosterId;
        public int victimRosterId;
        public int crimeKind;
        public float crimePressurePercent;
        public int readFlag;
        public int boloFlag;
        public int metroLineId;
        public int metroStationsCount;
        public int metroTunnelCells;
        public int metroLinesOpened;
    }
    public class MetroSaveNews
    {
        public int nextId;
        public int lastDailyBriefingDay = -1;
        public MetroSaveNewsArticleRow[]? articles;
    }
    public class MetroSaveMetroLineRow
    {
        public int lineId;
        public string displayName;
        public string namingPackId;
        public int colorIndex;
        public int capacity;
        public int schedule;
        public int visibleOnMap = 1;
        public string namingStyleId;
        public int transportKind;
    }
    public class MetroSaveMetroStationRow
    {
        public int x;
        public int y;
        public int depth;
        public int enabled;
    }
    public class MetroSaveMetroLineLinkRow
    {
        public int lineId;
        public int ax;
        public int ay;
        public int ad;
        public int bx;
        public int by;
        public int bd;
    }
    public class MetroSaveMetroLineRidershipRow
    {
        public int lineId;
        public float dailyRiders;
        public float pulseRiders;
    }
    public class MetroSaveMetroLines
    {
        public int nextLineId = 1;
        public string cityNamingPackId;
        public string cityNamingStyleId;
        public MetroSaveMetroLineRow[]? lines;
        public MetroSaveMetroLineLinkRow[]? linkOwners;
        public MetroSaveMetroStationRow[]? stations;
        /// <summary>Stations/tunnels under construction (v33+).</summary>
        public MetroSaveSubwayBuildRow[]? buildJobs;
        /// <summary>Intraday metro ridership totals (v35+).</summary>
        public int dailyRiders;
        public int pulseRiders;
        public MetroSaveMetroLineRidershipRow[]? ridershipByLine;
    }
    public class MetroSaveSubwayBuildRow
    {
        public int kind;
        public int ax;
        public int ay;
        public int ad;
        public int bx;
        public int by;
        public int bd;
        public int startDay;
        public int completeDay;
        public int tunnelSpanCells;
    }
}
