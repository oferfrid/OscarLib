/*
 * OscarLib
 * http://shaim.net/trac/oscarlib/
 * Copyright ©2005-2008, Chris Sammis
 * Licensed under the Lesser GNU Public License (LGPL)
 * http://www.opensource.org/osi3.0/licenses/lgpl-license.php
 * 
 */
using System;

namespace csammisrun.OscarLib
{
    /// <summary>
    /// Describes settable ICQ flags
    /// </summary>
    public enum ICQFlags
    {
        /// <summary>
        /// No special flags are set
        /// </summary>
        Normal = 0x0000,
        /// <summary>
        /// Client is Web Aware
        /// </summary>
        WebAware = 0x0001,
        /// <summary>
        /// Server can reveal client's IP address
        /// </summary>
        ShowIP = 0x0002,
        /// <summary>
        /// Today is the user's birthday
        /// </summary>
        Birthday = 0x0008,
        /// <summary>
        /// User has an ICQ homepage
        /// </summary>
        ActiveWebfront = 0x0020,
        /// <summary>
        /// Direct Connect is disabled
        /// </summary>
        DCDisabled = 0x0010,
        /// <summary>
        /// Direct Connect requires authorization
        /// </summary>
        DCAuthorizationOnly = 0x1000,
        /// <summary>
        /// Direct Connect with users on this client's contact list only
        /// </summary>
        DCContactsOnly = 0x2000
    }

    /// <summary>
    /// Describes the client's online state
    /// </summary>
    public enum ICQStatus
    {
        /// <summary>
        /// Client is online
        /// </summary>
        Online = 0x0000,
        /// <summary>
        /// Client is away
        /// </summary>
        Away = 0x0001,
        /// <summary>
        /// Client has set the "Do Not Disturb" flag
        /// </summary>
        DoNotDisturb = 0x0002,
        /// <summary>
        /// Client is not available
        /// </summary>
        NotAvailable = 0x0004,
        /// <summary>
        /// Client is occupied
        /// </summary>
        Occupied = 0x0010,
        /// <summary>
        /// Client is free for chat
        /// </summary>
        FreeForChat = 0x0020,
        /// <summary>
        /// Client is invisible
        /// </summary>
        Invisible = 0x0100,

        /*  Translations courtesy of lexeich  */
        /// <summary>
        /// Кушаю
        /// </summary>
        Eating = 0x2001,
        /// <summary>
        /// Злой
        /// </summary>
        Angry = 0x3000,
        /// <summary>
        /// Депрессия :(
        /// </summary>
        Depressed = 0x4000,
        /// <summary>
        /// Дома
        /// </summary>
        Home = 0x5000,
        /// <summary>
        /// На работе
        /// </summary>
        Working = 0x6000
    }

    /// <summary>
    /// ICQ Language Codes
    /// </summary>
    public enum LanguageList : ushort
    {
        Afrikaans = 55,
        Albanian = 58,
        Arabic = 1,
        Armenian = 59,
        Azerbaijani = 68,
        Belorussian = 72,
        Bhojpuri = 2,
        Bosnian = 56,
        Bulgarian = 3,
        Burmese = 4,
        Cantonese = 5,
        Catalan = 6,
        Chamorro = 61,
        Chinese = 7,
        Croatian = 8,
        Czech = 9,
        Danish = 10,
        Dutch = 11,
        English = 12,
        Esperanto = 13,
        Estonian = 14,
        Farsi = 15,
        Finnish = 16,
        French = 17,
        Gaelic = 18,
        German = 19,
        Greek = 20,
        Gujarati = 70,
        Hebrew = 21,
        Hindi = 22,
        Hungarian = 23,
        IcelAndic = 24,
        Indonesian = 25,
        Italian = 26,
        Japanese = 27,
        Khmer = 28,
        Korean = 29,
        Kurdish = 69,
        Lao = 30,
        Latvian = 31,
        Lithuanian = 32,
        Macedonian = 65,
        Malay = 33,
        MAndarin = 63,
        Mongolian = 62,
        Norwegian = 34,
        Persian = 57,
        Polish = 35,
        Portuguese = 36,
        Punjabi = 60,
        Romanian = 37,
        Russian = 38,
        Serbian = 39,
        Sindhi = 66,
        Slovak = 40,
        Slovenian = 41,
        Somali = 42,
        Spanish = 43,
        Swahili = 44,
        Swedish = 45,
        Tagalog = 46,
        Taiwanese = 64,
        Tamil = 71,
        Tatar = 47,
        Thai = 48,
        Turkish = 49,
        Ukrainian = 50,
        Urdu = 51,
        Vietnamese = 52,
        Welsh = 67,
        Yiddish = 53,
        Yoruba = 54,
        NOTHING = 0
    }

    /// <summary>
    /// Icq Interest Codes
    /// </summary>
    public enum InterestList : ushort
    {
        Fifties = 137,
        Sixties = 134,
        Seventies = 135,
        Eighties = 136,
        Art = 100,
        Astronomy = 128,
        AudioAndVisual = 147,
        Business = 125,
        BusinessServices = 146,
        Cars = 101,
        CelebrityFans = 102,
        Clothing = 130,
        Collections = 103,
        Computers = 104,
        Culture = 105,
        Ecology = 122,
        Entertainment = 139,
        FinanceAndCorporate = 138,
        Fitness = 106,
        HealthAndBeauty = 142,
        Hobbies = 108,
        HomeAutomation = 150,
        HouseholdProducts = 144,
        Games = 107,
        Government = 124,
        ICQHelp = 109,
        Internet = 110,
        Lifestyle = 111,
        MailOrderCatalog = 145,
        Media = 143,
        MoviesAndTV = 112,
        Music = 113,
        Mystics = 126,
        NewsAndMedia = 123,
        Outdoors = 114,
        Parenting = 115,
        Parties = 131,
        PetsAndAnimals = 116,
        Publishing = 149,
        Religion = 117,
        RetailStores = 141,
        Science = 118,
        Skills = 119,
        Socialscience = 133,
        Space = 129,
        SportingAndAthletic = 148,
        Sports = 120,
        Travel = 127,
        WebDesign = 121,
        Women = 132
    }

    /// <summary>
    /// Icq Country Codes
    /// </summary>
    public enum CountryList : uint
    {
        Other = 9999,
        Afghanistan = 93,
        Albania = 355,
        Algeria = 213,
        Andorra = 376,
        Angola = 244,
        Anguilla = 101,
        AntiguaAndBarbuda = 1021,
        Antilles = 5902,
        Argentina = 54,
        Armenia = 374,
        Aruba = 297,
        AscensionIslAnd = 247,
        Australia = 61,
        Austria = 43,
        Azerbaijan = 994,
        Bahamas = 103,
        Bahrain = 973,
        Bangladesh = 880,
        Barbados = 104,
        Barbuda = 120,
        Belarus = 375,
        Belgium = 32,
        Belize = 501,
        Benin = 229,
        Bermuda = 105,
        Bhutan = 975,
        Bolivia = 591,
        BosniaAndHerzegovina = 387,
        Botswana = 267,
        Brazil = 55,
        BritishVirginIslAnds = 106,
        Brunei = 673,
        Bulgaria = 359,
        BurkinaFaso = 226,
        Burundi = 257,
        Cambodia = 855,
        Cameroon = 237,
        Canada = 107,
        CanaryIslAnds = 178,
        CapeVerdeIslAnds = 238,
        CaymanIslAnds = 108,
        CentralAfricanRepublic = 236,
        Chad = 235,
        Chile, Republicof = 56,
        China = 86,
        ChristmasIslAnd = 672,
        CocosKeelingIslAnds = 6102,
        Colombia = 57,
        Comoros = 2691,
        CongoRepublicOfThe = 242,
        CongoDemocraticRepublicOfZaire = 243,
        CookIslAnds = 682,
        CostaRica = 506,
        CotedIvoireIvoryCoast = 225,
        Croatia = 385,
        Cuba = 53,
        Cyprus = 357,
        CzechRepublic = 42,
        Denmark = 45,
        DiegoGarcia = 246,
        Djibouti = 253,
        Dominica = 109,
        DominicanRepublic = 110,
        Ecuador = 593,
        Egypt = 20,
        ElSalvador = 503,
        EquatorialGuinea = 240,
        Eritrea = 291,
        Estonia = 372,
        Ethiopia = 251,
        FaeroeIslAnds = 298,
        FalklAndIslAnds = 500,
        Fiji = 679,
        FinlAnd = 358,
        France = 33,
        FrenchAntilles = 5901,
        FrenchGuiana = 594,
        FrenchPolynesia = 689,
        Gabon = 241,
        Gambia = 220,
        Georgia = 995,
        Germany = 49,
        Ghana = 233,
        Gibraltar = 350,
        Greece = 30,
        GreenlAnd = 299,
        Grenada = 111,
        Guadeloupe = 590,
        Guam, USTerritoryof = 671,
        Guatemala = 502,
        Guinea = 224,
        GuineaBissau = 245,
        Guyana = 592,
        Haiti = 509,
        Honduras = 504,
        HongKong = 852,
        Hungary = 36,
        IcelAnd = 354,
        India = 91,
        Indonesia = 62,
        IranIslamicRepublicof = 98,
        Iraq = 964,
        IrelAnd = 353,
        Israel = 972,
        Italy = 39,
        Jamaica = 112,
        Japan = 81,
        Jordan = 962,
        Kazakhstan = 705,
        Kenya = 254,
        Kiribati = 686,
        KoreaNorthKoreaDemocraticPeoplesRepublicOf = 850,
        KoreaSouthKoreaRepublicOf = 82,
        Kuwait = 965,
        Kyrgyzstan = 706,
        LaoPeoplesDemocraticRepublic = 856,
        Latvia = 371,
        Lebanon = 961,
        Lesotho = 266,
        Liberia = 231,
        LibyanArabJamahiriya = 218,
        Liechtenstein = 4101,
        Lithuania = 370,
        Luxembourg = 352,
        Macau = 853,
        MacedoniaFYROM = 389,
        Madagascar = 261,
        Malawi = 265,
        Malaysia = 60,
        Maldives = 960,
        Mali = 223,
        Malta = 356,
        MarshallIslAnds = 692,
        Martinique = 596,
        Mauritania = 222,
        Mauritius = 230,
        MayotteIslAnd = 269,
        Mexico = 52,
        Micronesia, FederatedStatesof = 691,
        MoldovaRepublicOf = 373,
        Monaco = 377,
        Mongolia = 976,
        Montserrat = 113,
        Morocco = 212,
        Mozambique = 258,
        Myanmar = 95,
        Namibia = 264,
        Nauru = 674,
        Nepal = 977,
        NetherlAnds = 31,
        NetherlAndsAntilles = 599,
        Nevis = 114,
        NewCaledonia = 687,
        NewZealAnd = 64,
        Nicaragua = 505,
        Niger = 227,
        Nigeria = 234,
        Niue = 683,
        NorfolkIslAnd = 6722,
        Norway = 47,
        Oman = 968,
        Pakistan = 92,
        Palau = 680,
        Panama = 507,
        PapuaNewGuinea = 675,
        Paraguay = 595,
        Peru = 51,
        Philippines = 63,
        PolAnd = 48,
        Portugal = 351,
        PuertoRico, CommonWealthof = 121,
        Qatar = 974,
        ReunionIslAnd = 262,
        Romania = 40,
        RotaIslAnd = 6701,
        Russia = 7,
        RwAnda = 250,
        SaintHelena = 290,
        SaintKitts = 115,
        SaintKittsAndNevis = 1141,
        SaintLucia = 122,
        SaintPierreAndMiquelon = 508,
        SaintVincentAndtheGrenadines = 116,
        SaipanIslAnd = 670,
        Samoa = 684,
        SanMarino = 378,
        SaoTomeAndPrincipe = 239,
        SaudiArabia = 966,
        ScotlAnd = 442,
        Senegal = 221,
        Seychelles = 248,
        SierraLeone = 232,
        Singapore = 65,
        Slovakia = 4201,
        Slovenia = 386,
        SolomonIslAnds = 677,
        Somalia = 252,
        SouthAfrica = 27,
        Spain = 34,
        SriLanka = 94,
        Sudan = 249,
        Suriname = 597,
        SwazilAnd = 268,
        Sweden = 46,
        SwitzerlAnd = 41,
        SyrianArabRepublic = 963,
        Taiwan = 886,
        Tajikistan = 708,
        Tanzania, UnitedRepublicof = 255,
        ThailAnd = 66,
        TinianIslAnd = 6702,
        Togo = 228,
        Tokelau = 690,
        Tonga = 676,
        TrinidadAndTobago = 117,
        Tunisia = 216,
        Turkey = 90,
        Turkmenistan = 709,
        TurksAndCaicosIslAnds = 118,
        Tuvalu = 688,
        UgAnda = 256,
        Ukraine = 380,
        UnitedArabEmirates = 971,
        UnitedKingdom = 44,
        Uruguay = 598,
        USA = 1,
        Uzbekistan = 711,
        Vanuatu = 678,
        VaticanCity = 379,
        Venezuela = 58,
        VietNam = 84,
        VirginIslAndsoftheUnitedStates = 123,
        Wales = 441,
        WallisAndFutunaIslAnds = 681,
        WesternSamoa = 685,
        Yemen = 967,
        Yugoslavia = 381,
        YugoslaviaMontenegro = 382,
        YugoslaviaSerbia = 3811,
        Zambia = 260,
        Zimbabwe = 263,
        NOTHING = 0
    }

    /// <summary>
    /// Icq Industry Codes
    /// </summary>
    public enum IndustryList : ushort
    {
        Agriculture = 2,
        Arts = 3,
        Construction = 4,
        ConsumerGoods = 5,
        CorporateServices = 6,
        Education = 7,
        Finance = 8,
        Government = 9,
        HighTech = 10,
        Legal = 11,
        Manufacturing = 12,
        Media = 13,
        MedicalAndHealthCare = 14,
        NonProfitOrganizationManagement = 15,
        Other = 19,
        Recreation, TravelAndEntertainment = 16,
        ServiceIndustry = 17,
        Transportation = 18,
        NOTHING = 0
    }

    /// <summary>
    /// Icq Martial Codes
    /// </summary>
    public enum MartialList : ushort
    {
        Single = 10,
        Closerelationships = 11,
        Engaged = 12,
        Married = 20,
        Divorced = 30,
        Separated = 31,
        Widowed = 40,
        Openrelationship = 50,
        Other = 255,
        NOTHING = 0
    }

    /// <summary>
    /// Icq Study-Level Codes
    /// </summary>
    public enum StudyLevelList : ushort
    {
        Associateddegree = 4,
        Bachelorsdegree = 5,
        Elementary = 1,
        Highschool = 2,
        Mastersdegree = 6,
        PhD = 7,
        Postdoctoral = 8,
        UniversityCollege = 3,
        NOTHING = 0
    }

    /// <summary>
    /// Contact Details; Short
    /// </summary>
    /// <remarks>strings are ascii</remarks>
    public class ShortUserInfo
    {
        /// <summary>
        /// ScreenName
        /// </summary>
        public string Screenname;

        /// <summary>
        /// Nickname
        /// </summary>
        public string Nickname;

        /// <summary>
        /// Firstname
        /// </summary>
        public string Firstname;

        /// <summary>
        /// Lastname
        /// </summary>
        public string Lastname;

        /// <summary>
        /// Email
        /// </summary>
        public string Email;


        /// <summary>
        /// Clone's the class
        /// </summary>
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    /// <summary>
    /// Contact Details; Full
    /// </summary>
    /// <remarks>most strings are utf-8</remarks>
    public class FullUserInfo
    {
        #region Names
        /// <summary>
        /// ScreenName
        /// </summary>
        public string Screenname = string.Empty;
        /// <summary>
        /// First name
        /// </summary>
        public string Firstname = string.Empty;
        /// <summary>
        /// Last name
        /// </summary>
        public string Lastname = string.Empty;
        /// <summary>
        /// Nickname
        /// </summary>
        public string Nickname = string.Empty;
        #endregion Names

        #region Addresses
        /// <summary>
        /// Home street address
        /// </summary>
        public string HomeAddress = string.Empty;
        /// <summary>
        /// Home postal code
        /// </summary>
        public string HomeZip = string.Empty;
        /// <summary>
        /// Home city
        /// </summary>
        public string HomeCity = string.Empty;
        /// <summary>
        /// Home country
        /// </summary>
        public CountryList HomeCountry = CountryList.NOTHING;
        /// <summary>
        /// Home state
        /// </summary>
        public string HomeState = string.Empty;

        /// <summary>
        /// Origin city
        /// </summary>
        public string OriginCity = string.Empty;
        /// <summary>
        /// Origin country
        /// </summary>
        public CountryList OriginCountry = CountryList.NOTHING;
        /// <summary>
        /// Origin state
        /// </summary>
        public string OriginState = string.Empty;
        #endregion Addresses

        #region Phone
        /// <summary>
        /// Home phone number
        /// </summary>
        public string HomePhone = string.Empty;
        /// <summary>
        /// Work phone number
        /// </summary>
        public string WorkPhone = string.Empty;

        /// <summary>
        /// Mobile phone
        /// </summary>
        public string MobilePhone = string.Empty;

        /// <summary>
        /// Home fax number
        /// </summary>
        public string HomeFax = string.Empty;
        /// <summary>
        /// Work fax number
        /// </summary>
        public string WorkFax = string.Empty;
        #endregion Phone

        #region Email
        /// <summary>
        /// Email
        /// </summary>
        public string Email = string.Empty;

        /// <summary>
        /// Email addresses
        /// </summary>
        public string[] EmailAddresses = null;
        #endregion Email

        #region Work
        /// <summary>
        /// Work position
        /// </summary>
        public string WorkPosition = string.Empty;
        /// <summary>
        /// Work company
        /// </summary>
        public string WorkCompany = string.Empty;
        /// <summary>
        /// Work department
        /// </summary>
        public string WorkDepartment = string.Empty;
        /// <summary>
        /// Work website
        /// </summary>
        public string WorkWebsite = string.Empty;
        /// <summary>
        /// Work industry
        /// </summary>
        public IndustryList WorkIndustry = IndustryList.NOTHING;
        /// <summary>
        /// Work street address
        /// </summary>
        public string WorkAddress = string.Empty;
        /// <summary>
        /// Work city
        /// </summary>
        public string WorkCity = string.Empty;
        /// <summary>
        /// Work state
        /// </summary>
        public string WorkState = string.Empty;
        /// <summary>
        /// Work postal code
        /// </summary>
        public string WorkZip = string.Empty;
        /// <summary>
        /// Work country
        /// </summary>
        public CountryList WorkCountry = CountryList.NOTHING;
        #endregion Work

        #region Education
        /// <summary>
        /// Study level
        /// </summary>
        [Obsolete("This property is obsolete and can't be set at Icq-Server.")]
        public StudyLevelList StudyLevel = StudyLevelList.NOTHING;
        /// <summary>
        /// Study institute
        /// </summary>
        [Obsolete("This property is obsolete and can't be set at Icq-Server.")]
        public string StudyInstitute = string.Empty;
        /// <summary>
        /// Study degree
        /// </summary>
        [Obsolete("This property is obsolete and can't be set at Icq-Server.")]
        public string StudyDegree = string.Empty;
        /// <summary>
        /// Study year
        /// </summary>
        [Obsolete("This property is obsolete and can't be set at Icq-Server.")]
        public ushort StudyYear = 0;
        #endregion Education

        #region Interests
        /// <summary>
        /// InterestInfo
        /// </summary>
        public struct InterestInfo
        {
            /// <summary>
            /// Info
            /// </summary>
            public string Info;

            /// <summary>
            /// Category
            /// </summary>
            public InterestList Category;
        }
        /// <summary>
        /// Interests
        /// </summary>
        public InterestInfo[] InterestInfos = null;
        #endregion Interests

        #region Additional
        /// <summary>
        /// Timezone
        /// </summary>
        public short Timezone = 0;
        /// <summary>
        /// Gender
        /// </summary>
        public char Gender = '\0';
        /// <summary>
        /// Website
        /// </summary>
        public string Website = string.Empty;
        /// <summary>
        /// Birthday
        /// </summary>
        public DateTime Birthday = DateTime.MinValue;
        /// <summary>
        /// Age
        /// </summary>
        public byte Age = 0;
        /// <summary>
        /// Language 1
        /// </summary>
        public LanguageList Language1 = LanguageList.NOTHING;
        /// <summary>
        /// Language 2
        /// </summary>
        public LanguageList Language2 = LanguageList.NOTHING;
        /// <summary>
        /// Language 3
        /// </summary>
        public LanguageList Language3 = LanguageList.NOTHING;
        /// <summary>
        /// Marital status
        /// </summary>
        public MartialList MaritalStatus = MartialList.NOTHING;
        /// <summary>
        /// About
        /// </summary>
        public string About = string.Empty;
        #endregion Additional

        #region Status,Privacy
        /// <summary>
        /// Status note
        /// </summary>
        public string StatusNote = string.Empty;
        /// <summary>
        /// Privacy level
        /// </summary>
        public ushort PrivacyLevel = 0;
        /// <summary>
        /// Auth
        /// </summary>
        public ushort Auth = 0;
        /// <summary>
        /// Web aware
        /// </summary>
        public byte WebAware = 0;
        /// <summary>
        /// Allow spam
        /// </summary>
        public byte AllowSpam = 0;
        /// <summary>
        /// Code Page
        /// </summary>
        public ushort CodePage = 0;
        #endregion Status,Privacy

        /// <summary>
        /// Clone's the class
        /// </summary>
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}