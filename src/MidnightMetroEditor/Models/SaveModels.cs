namespace MidnightMetroEditor.Models;

public sealed class CitySimSaveFile
{
    public int version = 2;
    public string? savedUtc;
    public int gridStaticRevision;
    public bool gridStaticExternal;
    public CitySimSaveSession? session;
    public List<SaveCell> cells = new();
    public List<SaveCitizen> citizens = new();
    public SaveGridBulk? grid;
    public SaveResidentBulk? residents;
    public List<SaveCitizen> agents = new();
    public SaveDeceasedBulk? deceased;
    public List<SaveHonorEntry> honorWall = new();
    public CitySimSaveBudget? budget;
    public CitySimSaveGangs? gangs;
    public CitySimSavePlayerAgency? playerAgency;
    public List<SaveCriminalCase> criminalCases = new();
    public List<CityMetricsDayRecord> metricsHistory = new();
}

public sealed class SaveGridBulk
{
    public int width;
    public int height;
    public int[]? type;
    public int[]? zone;
    public float[]? crimeLevel;
    public float[]? policeInfluence;
    public float[]? patrolBoost;
    public float[]? fireInfluence;
    public float[]? firePatrolBoost;
    public float[]? medicalInfluence;
    public float[]? medicalPatrolBoost;
    public int[]? population;
    public int[]? wealth;
    public int[]? ownerRosterId;
    public int[]? activeCrimeTier;
    public float[]? crimeTimerHours;
    public int[]? crimeFlags;
    public float[]? postSuccessHours;
    public int[]? preJailType;
    public int[]? wealthLostToCrime;
    public float[]? jailHoursRemaining;
    public float[]? prosperousHours;
    public int[]? gangId;
    public int[]? preGangType;
    public float[]? policeCoverageScale;
    public int[]? fireCallTier;
    public int[]? medicalCallTier;
    public int[]? trafficIncident;
    public float[]? trafficIncidentHours;
    public int[]? trafficFireTier;
    public int[]? trafficMedicalTier;
    public int[]? crimeSceneFlags;
    public int[]? packedCrimeSceneSuspectOfficer;
    public int[]? packedCrimeSceneMedicCaptured;
    public int[]? crimeSceneDisposition;
    public int[]? crimeSceneVictimInjury;
    public int[]? crimeIncidentStage;
    public int[]? packedFacilityAnchor;
    public int[]? jailedSuspectRosterId;
    public int[]? kidnapFacilityClosed;
    public float[]? businessHealth;
    public int[]? businessClosed;
    public int[]? businessClosedDays;
    public int[]? upgradePendingDays;
    public int[]? lotMixTier;
    public int[]? lotMixTrack;
}

public sealed class SaveResidentBulk
{
    public int[]? rosterId;
    public float[]? ageYears;
    public float[]? education;
    public int[]? packedHome;
    public int[]? packedWorkplace;
    public int[]? personalWealth;
    public int[]? flags;
    public int[]? givenNameId;
    public int[]? familyNameId;
    public int[]? nicknameId;
    public int[]? birthFamilyNameId;
    public int[]? familyId;
    public int[]? spouseRosterId;
    public int[]? biologicalMotherRosterId;
    public int[]? biologicalFatherRosterId;
    public int[]? parent1RosterId;
    public int[]? parent2RosterId;
    public int[]? pregnancyDaysRemaining;
    public int[]? pendingFatherRosterId;
    public int[]? sexualAssaultVictimCount;
    public int[]? sexualIntercourseCount;
    public int[]? sexualBlowjobCount;
    public int[]? sexualAnalCount;
    public int[]? sexualVaginalCount;
    public string[]? packedSexualPartnerRosterIds;
    public int[]? gangBrothelConsecutiveCount;
    public int[]? gangBrothelLastWorkDay;
    public string[]? packedGangBrothelKidsAtRank;
    public float[]? injuryPenaltyYears;
    public float[]? lifespanModifierYears;
    public float[]? householdIncomeMultiplier;
    public int[]? gangAffiliationId;
    public int[]? secondaryOccupation;
    public int[]? secondaryOccupationGangId;
    public int[]? sceneInjuryLevel;
    public int[]? packedSceneInjuryBuilding;
    public int[]? kidnapCaptorRosterId;
    public int[]? packedKidnapHideout;
    public int[]? lastKidnapPerpetratorRosterId;
    public string[]? geneticDna;
    public int[]? appearanceSex;
    public int[]? appearanceMaleHair;
    public int[]? appearanceFemaleHair;
    public int[]? appearanceHairColor;
    public float[]? appearanceHairGreyTint;
    public int[]? appearanceBodyType;
    public int[]? appearanceEthnicity;
    public int[]? appearanceCupSize;
    public float[]? appearanceFemaleBust;
    public float[]? appearanceFemaleWaist;
    public float[]? appearanceFemaleButt;
    public float[]? appearanceHeightMeters;
}

public sealed class SaveDeceasedBulk
{
    public int[]? rosterId;
    public int[]? occupation;
    public float[]? ageYears;
    public float[]? education;
    public int[]? packedHome;
    public int[]? packedWorkplace;
    public int[]? givenNameId;
    public int[]? familyNameId;
    public int[]? nicknameId;
    public int[]? birthFamilyNameId;
    public int[]? familyId;
    public int[]? spouseRosterId;
    public int[]? biologicalMotherRosterId;
    public int[]? biologicalFatherRosterId;
    public int[]? parent1RosterId;
    public int[]? parent2RosterId;
    public int[]? flags;
    public string[]? geneticDna;
    public int[]? appearanceSex;
    public int[]? appearanceMaleHair;
    public int[]? appearanceFemaleHair;
    public int[]? appearanceHairColor;
    public float[]? appearanceHairGreyTint;
    public int[]? appearanceBodyType;
    public int[]? appearanceEthnicity;
    public int[]? appearanceCupSize;
    public float[]? appearanceFemaleBust;
    public float[]? appearanceFemaleWaist;
    public float[]? appearanceFemaleButt;
    public float[]? appearanceHeightMeters;
}

public sealed class CitySimSaveSession
{
    public int gridWidth;
    public int gridHeight;
    public float cellSize;
    public float originX;
    public float originY;
    public float originZ;
    public int randomSeed;
    public int streetSpacing;
    public int day;
    public float tickTimer;
    public float tickInterval;
    public bool runSimulation;
    public int activeOverlay;
    public int nextCitizenRosterId;
    public string? cityName;
    public int gridStaticRevision;
}

public sealed class SaveCoord
{
    public int x;
    public int y;
}

public sealed class SaveCell
{
    public int x;
    public int y;
    public int type;
    public int zone;
    public float crimeLevel;
    public float policeInfluence;
    public float patrolBoost;
    public float fireInfluence;
    public float firePatrolBoost;
    public float medicalInfluence;
    public float medicalPatrolBoost;
    public int population;
    public int wealth;
    public int maxFloors;
    public int builtFloors;
    public int externalWorkers;
    public float groundPollution;
    public float airPollution;
    public float noisePollution;
    public float neighborhoodQuality;
    public int ownerRosterId;
    public int activeCrimeTier;
    public float crimeTimerHours;
    public bool crimeSucceeded;
    public float postSuccessHours;
    public int preJailType;
    public int wealthLostToCrime;
    public float jailHoursRemaining;
    public float prosperousHours;
    public bool nextCrimeForcedTier1;
    public int gangId;
    public int preGangType;
    public float policeCoverageScale;
    public int fireCallTier;
    public int medicalCallTier;
    public int trafficIncident;
    public float trafficIncidentHours;
    public int trafficFireTier;
    public int trafficMedicalTier;
    public bool crimeAwaitingPlayerDispatch;
    public bool playerMayorDispatched;
    public bool playerDeclinedDispatch;
    public bool crimeSceneInspected;
    public bool crimeSceneInterviewed;
    public bool crimeSceneMedicCalled;
    public bool crimeSceneInspectPassed;
    public bool crimeSceneTalkPassed;
    public bool crimeSceneMedicPassed;
    public bool crimeSceneArrestPassed;
    public int crimeSceneSuspectRosterId;
    public int crimeSceneOfficerPinnedRosterId;
    public int crimeSceneMedicPinnedRosterId;
    public int crimeSceneDisposition;
    public int crimeSceneVictimInjury;
    public bool crimeSceneEmsTreated;
    public bool crimeSceneResolved;
    public bool crimeSuspectFled;
    public bool crimeSceneApExhausted;
    public bool crimeSceneBackupRequested;
    public int crimeSceneOfficerCapturedRosterId;
    public int crimeIncidentStage;
    public int facilityAnchorX = -1;
    public int facilityAnchorY = -1;
    public int jailedSuspectRosterId;
    public bool kidnapFacilityClosed;
    public float businessHealth;
    public bool businessClosed;
    public int businessClosedDays;
    public int upgradePendingDays;
    public int lotMixTier;
    public int lotMixTrack;
}

public sealed class SaveCitizen
{
    public int occupation;
    public float ageYears;
    public float education;
    public bool droppedOutOfSchool;
    public bool homeschooled;
    public int personalWealth;
    public bool employed;
    public SaveCoord? homeBuilding;
    public SaveCoord? workplace;
    public int rosterId;
    public int shiftSlotIndex;
    public int gangId;
    public int gangAffiliationId;
    public SaveCoord? stationCoord;
    public SaveCoord? homeStreet;
    public SaveCoord? coord;
    public SaveCoord? nextCoord;
    public float progress;
    public int phase;
    public float stamina;
    public int patrolCellsThisShift;
    public int respawnTimer;
    public float downtimeSecondsRemaining;
    public bool prioritizeLowCoverage;
    public string? displayName;
    public int givenNameId = -1;
    public int familyNameId = -1;
    public int nicknameId = -1;
    public int nicknameKind;
    public int birthFamilyNameId = -1;
    public int familyId;
    public int spouseRosterId;
    public int marriageKind;
    public int biologicalMotherRosterId;
    public int biologicalFatherRosterId;
    public int parent1RosterId;
    public int parent2RosterId;
    public bool isPregnant;
    public int pregnancyDaysRemaining;
    public int pendingFatherRosterId;
    public int pendingBiologicalFatherKind;
    public int biologicalFatherKind;
    public int sexualAssaultVictimCount;
    public int sexualIntercourseCount;
    public int sexualBlowjobCount;
    public int sexualAnalCount;
    public int sexualVaginalCount;
    public List<int> sexualPartnerRosterIds = new();
    public int gangBrothelConsecutiveCount;
    public int gangBrothelLastWorkDay = -1;
    public string? packedGangBrothelKidsAtRank;
    public int rapport;
    public int heat;
    public int playerDuty;
    public float shiftOffsetHours;
    public int trainingRank;
    public int trainingTowardRank;
    public int trainingDaysRemaining;
    public int trainingDaysTotal;
    public int crimesCommittedToday;
    public SaveCoord? reclaimTarget;
    public SaveCoord? crimeResponseBuilding;
    public bool crimeResponseActive;
    public SaveCoord? serviceCallBuilding;
    public bool serviceCallActive;
    public bool onOvertimeShift;
    public bool pendingLayoff;
    public bool pendingMedEvac;
    public bool pendingReturnToPrecinct;
    public SaveCoord? medEvacStreet;
    public SaveCoord? travelTargetStreet;
    public float treatmentSecondsRemaining;
    public float trainingEffectPerRank;
    public int sceneInjuryLevel;
    public float injuryPenaltyYears;
    public float lifespanModifierYears;
    public bool isRetired;
    public SaveCoord? sceneInjuryBuilding;
    public float householdIncomeMultiplier = 1f;
    public List<SaveCoord> roamStreets = new();
    public int roamIndex;
    public bool isKidnapped;
    public bool isGangBrokenIn;
    public bool isGangBrothelFreeUse;
    public bool paternityDnaLocked;
    public float captivityBreakInHours;
    public SaveCoord? kidnapHideoutBuilding;
    public int kidnapCaptorRosterId;
    public int lastKidnapPerpetratorRosterId;
    public bool kidnapActiveAsCaptor;
    public bool neighborhoodPredator;
    public int childhoodAbuseExposure;
    public bool raisedInFosterCare;
    public int jailGangId;
    public int secondaryOccupation;
    public int secondaryOccupationGangId;
    public int forcedProstitutionGangId;
    public bool hasAppearance;
    public int characterSex;
    public int maleHair;
    public int femaleHair;
    public int hairColor;
    public float hairGreyTint;
    public int bodyType;
    public int ethnicity;
    public string? geneticDna;
    public int cupSize;
    public float femaleBust;
    public float femaleWaist;
    public float femaleButt;
    public float heightMeters;
}

public sealed class CitySimSaveBudget
{
    public float balance;
    public float startingBalance;
    public float populationTaxPerDay;
    public int maxUnitsPerStation;
    public float policePer1000Residents;
    public int residentCountForStaffing;
    public float lastDayTaxIncome;
    public float lastDayHousingTax;
    public float lastDayInheritanceTax;
    public float lastDayStationUpkeep;
    public float lastDayUnitUpkeep;
    public float lastDayNet;
    public float lastDayPoliceRaids;
    public float previousDayTaxIncome;
    public float lastDayServiceBudgetCharge;
    public float pendingHousingTax;
    public float pendingEducationFees;
    public List<SaveStationUnits> staffing = new();
    public List<SaveStationUpgrade> upgrades = new();
    public List<BudgetDayRecord> history = new();
    public float policeSalaryPct;
    public float policeTrainingPct;
    public float policeOtPct;
    public float policeCapitalPct;
    public float fireSalaryPct;
    public float fireTrainingPct;
    public float fireOtPct;
    public float fireCapitalPct;
    public float medicalSalaryPct;
    public float medicalTrainingPct;
    public float medicalOtPct;
    public float medicalCapitalPct;
    public float zoneResidentialPct;
    public float zoneCommercialPct;
    public float zoneOfficePct;
    public float zoneIndustrialPct;
    public float zoneMixedPct;
    public float educationSystemScale = 1f;
    public float courtSystemScale = 1f;
    public float educationPercentOfTax;
    public float justicePercentOfTax;
    public float welfareFundPercentOfTax = 1f;
    public float welfareFundBalance;
}

public sealed class SaveStationUnits
{
    public int x;
    public int y;
    public int service;
    public int units;
}

public sealed class SaveStationUpgrade
{
    public int x;
    public int y;
    public int capacityLevel;
    public int trainingRank;
    public int budgetTrainingRank;
}

public sealed class BudgetDayRecord
{
    public int day;
    public float taxIncome;
    public float propertyTax;
    public float housingTax;
    public float educationFeeIncome;
    public float fineIncome;
    public float inheritanceTax;
    public float stationUpkeep;
    public float unitUpkeep;
    public float serviceBudgetCharge;
    public float policeServiceCost;
    public float fireServiceCost;
    public float medicalServiceCost;
    public float policeSalary;
    public float policeTraining;
    public float policeOvertime;
    public float fireSalary;
    public float fireTraining;
    public float fireOvertime;
    public float medicalSalary;
    public float medicalTraining;
    public float medicalOvertime;
    public float policeCourtStaff;
    public float policeJailGuards;
    public float civicFacilityCost;
    public float civicCourthouseBuilding;
    public float civicJudgeSalary;
    public float civicJailBuilding;
    public float civicEducationTeachers;
    public float civicEducationResidents;
    public float policeRaids;
    public float net;
    public float householdWorkIncome;
    public float householdShopSpending;
    public float householdHousingTax;
    public float householdRobberyLoss;
    public float gangDivertedWealth;
    public float householdEducationFees;
    public float householdAdoptionSpend;
}

public sealed class CitySimSaveGangs
{
    public int nextGangId;
    public List<SaveGang> gangs = new();
    public List<SaveGangWar> wars = new();
    public List<SaveGangReclaim> reclaims = new();
    public List<SaveGangNameReservation> nameReservations = new();
}

public sealed class SaveGang
{
    public int id;
    public int nameId;
    public int tier;
    public int memberCount;
    public SaveCoord? anchor;
    public float collectedWealth;
    public float lastDayProstitutionRevenue;
    public int forcedProstituteCount;
    public SaveCoord? hqBuilding;
    public int leaderKidnapTargetRosterId;
    public int leaderBribeTargetRosterId;
    public int leaderOrderPriority;
    public int dominantEthnicity;
    public int ethnicHomophily;
    public int crossEthnicTargeting;
    public List<SaveCoord> buildings = new();
    public float lastDayHqDrugRevenue;
    public float lastDayHqBrothelRevenue;
    public float lastDayStripBarRevenue;
    public float lastDayStreetDrugRevenue;
    public float lastDayStreetRobberyRevenue;
    public float lastDayForcedProstitutionRevenue;
    public float lastDayAdultEntertainmentRevenue;
    public bool licensedAdultVenueActive;
    public float lastDayLicensedVenueGross;
    public float lastDayLicensedVenueTaxPaid;
    public float lastDayLicensedVenueNet;
    public int licensedVenueWorkerCount;
    public float lastDayRansomRevenue;
    public float lastDayTerritorySiphon;
    public float lastDayBribesPaid;
    public float lastDayBribeUpkeepPaid;
    public float lastDayMemberUpkeep;
    public List<int> hqBrothelRoomRosterIds = new();
    public List<int> hqBrothelRoomClientIds = new();
    public List<int> hqBrothelRoomSecondClientIds = new();
    public List<SaveGangHqBrothelVisit> hqBrothelVisits = new();
    public List<SaveGangCivWorkerOpsEntry> civWorkerOpsLog = new();
    public List<SaveGangBrothelWorkerRetirement> brothelWorkerRetirements = new();
    public List<SaveGangBrothelWorkerPendingDisposition> brothelWorkerPendingDispositions = new();
}

public sealed class SaveGangBrothelWorkerPendingDisposition
{
    public int dayQueued;
    public int workerRosterId;
    public string? displayName;
    public int occupation;
    public int cullReason;
    public int successorRosterId;
    public string? successorName;
    public int effectiveRank;
    public string? rankLabel;
}

public sealed class SaveGangBrothelWorkerRetirement
{
    public int day;
    public int workerRosterId;
    public string? displayName;
    public float ageYears;
    public int occupation;
    public string? workerKind;
    public int streakRank;
    public int effectiveRank;
    public int consecutiveShifts;
    public string? packedKidsAtRank;
    public int disposal;
    public int cullReason;
    public string? successorName;
    public bool isPregnant;
    public int pregnancyDaysRemaining;
    public string? packedKidsByAgeGroup;
}

public sealed class SaveGangCivWorkerOpsEntry
{
    public int day;
    public int rosterId;
    public int occupation;
    public int kind;
    public float revenueToGang;
}

public sealed class SaveGangHqBrothelVisit
{
    public int day;
    public int shiftIndex;
    public int brothelFloorIndex = 1;
    public int prostituteRosterId;
    public int clientRosterId;
    public int clientOccupation;
    public float serviceHours;
    public float revenueToGang;
    public bool isCivWorker;
}

public sealed class SaveGangNameReservation
{
    public int gangId;
    public int nameId;
}

public sealed class SaveGangWar
{
    public int blockX;
    public int blockY;
    public int gangAId;
    public int gangBId;
    public int daysActive;
    public int lastOutcome;
}

public sealed class SaveGangReclaim
{
    public int gangId;
    public int buildingX;
    public int buildingY;
    public float hoursRemaining;
    public List<int> officerRosterIds = new();
}

public sealed class SaveKidnapMission
{
    public int victimRosterId;
    public int captorRosterId;
    public SaveCoord? captureBuilding;
    public SaveCoord? hideoutBuilding;
    public SaveCoord? precinctStation;
    public int buildingsSearched;
    public int searchQueueVersion;
    public int startedDay;
    public float startedSimHour;
    public bool captorIdentityKnownToPolice;
    public List<int> searchQueueX = new();
    public List<int> searchQueueY = new();
    public List<int> trappedStaffRosterIds = new();
    public int tipBlockX = -999;
    public int tipBlockY = -999;
    public int tipsterRosterId;
    public bool tipPointsToHideout;
    public bool tipRewardPaid;
    public int lastTipRollDay;
}

public sealed class SaveMissingPersonAlert
{
    public int victimRosterId;
    public int captorRosterId;
    public SaveCoord? captureBuilding;
    public SaveCoord? hideoutBuilding;
    public int reportedDay;
}

public sealed class SaveRecentKidnapVictim
{
    public int victimRosterId;
    public int captorRosterId;
    public SaveCoord? precinctStation;
    public SaveCoord? captureBuilding;
    public int releasedDay;
    public bool viaRansom;
    public bool captorIdentityKnownToPolice;
}

public sealed class CitySimSavePlayerAgency
{
    public int actionsPerDay;
    public int actionsRemaining;
    public int patrolFocus;
    public int citywideStationDuty;
    public bool autoDispatchCrimes;
    public int streetAutoAnswerTier1;
    public int streetAutoAnswerTier2;
    public int streetAutoAnswerTier3;
    public int gangCaptureDefault;
    public int gangLeaderOrderDefault;
    public List<long> investigationClues = new();
    public List<SaveKidnapMission> kidnapMissions = new();
    public List<SaveMissingPersonAlert> missingPersonAlerts = new();
    public List<SaveRecentKidnapVictim> recentKidnapVictims = new();
    public int externalPoliceContractGangId;
    public List<int> externalPoliceContractRosterIds = new();
    public int externalPoliceContractDeployedDay;
    public int externalPoliceContractDrawdownStartedDay;
    public int externalPoliceContractPeakRoster;
}

public sealed class SaveHonorEntry
{
    public int rosterId;
    public string? displayName;
    public int occupation;
    public SaveCoord? stationCoord;
    public int dayFallen;
    public int trainingRank;
}

public sealed class SaveCriminalCase
{
    public int suspectRosterId;
    public SaveCoord? crimeBuilding;
    public int crimeTier;
    public bool evidenceCollected;
    public bool victimStatementFiled;
    public string? victimStatementText;
    public bool dnaSampleCollected;
    public bool fingerprintCollected;
    public bool victimDescriptionAccurate = true;
    public bool boloActive;
    public float boloPatrolSpotBoost;
    public int detectionPath;
    public int crossReferencedCaseCount;
    public int disposition;
    public bool pendingCourtReview;
    public bool incarcerated;
    public SaveCoord? jailCellCoord;
    public int filedDay;
    public string? suspectDisplayName;
    public string? suspectDescription;
    public SaveCoord? handlingPrecinctStation;
    public int handlingOfficerRosterId;
    public string? crimeTypeLabel;
    public bool streetEncounter;
    public SaveCoord? encounterStreetCoord;
    public string? patrolStatementText;
    public int secondaryOfficerRosterId;
    public List<CriminalCaseHistoryEntry> history = new();
    public List<int> involvedOfficerRosterIds = new();
    public List<SaveCoord> linkedCrimeScenes = new();
    public bool convicted;
    public string? sentenceText;
    public int trialDay;
    public int arrestDay;
    public int sentenceEndDay;
    public int jailGangId;
    public bool captorIdentityKnownToPolice;
    public int justicePhase;
    public int bookingDay;
    public int courtHoldDay;
    public int assignedJudgeRosterId;
    public int victimWitnessRosterId;
    public bool highProfileCase;
    public float holdCompensationPaid;
    public int bribingGangId;
    public int bribedOfficerRosterId;
    public int bribedJudgeRosterId;
    public int briberyDay;
    public bool evidenceCompromised;
    public bool evidenceTamperExposed;
    public bool homicideBodyDump;
    public bool bodyDiscovered;
    public bool coldCaseArchived;
    public bool suspectOnRecord;
    public int victimRosterId;
    public int victimDnaMarker;
    public int investigationDaysOpen;
}

public sealed class CriminalCaseHistoryEntry
{
    public int day;
    public int kind;
    public string? summary;
    public int locationX;
    public int locationY;
    public bool hasCrimeBuilding;
    public int crimeBuildingX;
    public int crimeBuildingY;
    public int primaryOfficerRosterId;
    public int secondaryOfficerRosterId;
    public bool pendingTrial;
}

public sealed class CityMetricsDayRecord
{
    public int day;
    public float treasury;
    public float netDay;
    public string? ledgerLabel;
    public int population;
    public int housingVacant;
    public int housingCapacity;
    public int births;
    public int deaths;
    public int immigrated;
    public int emigrated;
    public int jobSlotsFilled;
    public int jobSlotsTotal;
    public int unemployed;
    public float avgCrime;
    public int jailed;
    public int gangs;
    public int criminals;
    public int kidnapped;
    public int police;
    public int externalPolice;
    public int policeTarget;
    public int fire;
    public int fireTarget;
    public int medical;
    public int medicalTarget;
    public float staffingFill;
    public float residentialDemand;
    public float commercialDemand;
    public float industrialDemand;
    public float officeDemand;
    public float estTaxDaily;
}
