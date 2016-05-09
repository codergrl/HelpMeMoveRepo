using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Tasks.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace HelpMeMove.ViewModels
{
    /// <summary>
    /// Main view model handles most of the logic of the application
    /// </summary>
    class MainViewModel: INotifyPropertyChanged
    {
        public SurveyResult surveyResult = new SurveyResult();

        public MainViewModel()
        {
            //set up initial map 
            _map = new Map();
            Uri uri = new Uri("http://services.arcgisonline.com/arcgis/rest/services/NatGeo_World_Map/MapServer");
            var baseLayer = new ArcGISTiledMapServiceLayer(uri);
            _map.Layers.Add(baseLayer);

            // set initial extent to Portland 
            MapPoint mapPoint = new MapPoint(-122.677461, 45.521661, SpatialReferences.Wgs84);
            ViewpointCenter initViewPoint = new ViewpointCenter(mapPoint, 100000);
            _map.InitialViewpoint = initViewPoint;

            _canExecute = true;
            //do not display the progress bar or the results list
            ProgressOverlayVisibility = Visibility.Collapsed;
            TapestryLinksVisibility = Visibility.Collapsed;
        }

        #region Declare Properties 

        //get a handle on the map for the application
        private Map _map;
        public Map Map
        {
            get { return _map; }
            set { if (_map != value)
                {
                    _map = value;
                    NotifyPropertyChanged("Map");
                }
            }
        }

        //visibility property for the progress bar 
        private Visibility _progressOverlayVisibility = Visibility.Visible;
        public Visibility ProgressOverlayVisibility
        {
            get
            {
                return _progressOverlayVisibility;
            }
            set
            {
                if (_progressOverlayVisibility != value)
                {
                    _progressOverlayVisibility = value;
                    NotifyPropertyChanged("ProgressOverlayVisibility");
                }
            }
        }

        //visibility property for the results listbox
        private Visibility _tapestryLinksVisibility;
        public Visibility TapestryLinksVisibility
    {
            get { return _tapestryLinksVisibility; }
            set {
                if (_tapestryLinksVisibility != value)
                {
                    _tapestryLinksVisibility = value;
                    NotifyPropertyChanged("TapestryLinksVisibility");
                }
            }
        }

        //collection of the returned tapestry objects
        private ObservableCollection<Tapestry> tapestryNames;
        public ObservableCollection<Tapestry> TapestryNames
        {
            get { return tapestryNames; }
            set
            {
                if (tapestryNames != value)
                {
                    tapestryNames = value;
                    NotifyPropertyChanged("TapestryNames");
                }
            }
        }

        #region Properties for Combo Boxes

        //property holding the age value
        protected string selectedAgeValue;
        public string SelectedAgeValue
        {
            get { return selectedAgeValue; }
            set
            {
                if (selectedAgeValue != value)
                {
                    selectedAgeValue = value;
                    surveyResult.Age = selectedAgeValue;
                }
            }
        }
        
        //property holding the marital status value
        protected string selectedMaritalStatus;
        public string SelectedMaritalStatus
        {
            get { return selectedMaritalStatus; }
            set
            {
                if (selectedMaritalStatus != value)
                {
                    selectedMaritalStatus = value;
                    surveyResult.MaritalStatus = selectedMaritalStatus;
                }
            }
        }

        //property holding the household size value
        protected string selectedHouseholdSizeValue;
        public string SelectedHouseholdSizeValue
        {
            get { return selectedHouseholdSizeValue; }
            set
            {
                if (selectedHouseholdSizeValue != value)
                {
                    selectedHouseholdSizeValue = value;
                    surveyResult.HouseholdSize = selectedHouseholdSizeValue;
                }
            }
        }
        
        //property holding the income value
        protected string selectedIncomeValue;
        public string SelectedIncomeValue
        {
            get { return selectedIncomeValue; }
            set
            {
                if (selectedIncomeValue != value)
                {
                    selectedIncomeValue = value;
                    surveyResult.HouseholdIncome = selectedIncomeValue;
                }
            }
        }

        #endregion

        #region Properties for Radio Buttons

        //TODO: make this part more efficient, it should not require 2 separate properties

        private bool isMale;
        public bool IsMale
        {
            get { return isMale; }
            set
            {
                if (isMale != value)
                {
                    isMale = value;                   
                }
                if (isMale == true)
                    surveyResult.Gender = "Male";
                else
                    surveyResult.Gender = "Female";
            }
        }

        private bool isFemale;
        public bool IsFemale
        {
            get { return isFemale; }
            set
            {
                if (isFemale != value)
                {
                    isFemale = value;
                }
                if (isFemale == true)
                    surveyResult.Gender = "Female";
                else
                    surveyResult.Gender = "Male";
            }
        }

        #endregion

        #endregion

        // property bound to the click command for the Submit button
        private ICommand _clickCommand;
        public ICommand ClickCommand
        {
            get
            {
                return _clickCommand ?? (_clickCommand = new CommandHandler((x) => SubmitButtonAction(x), _canExecute));
            }
        }

        private bool _canExecute;
        /// <summary>
        /// Queries the Demographics layer for all the features that meet the user selected criteria
        /// </summary>
        /// <param name="mapView">Main MapView</param>
        /// <returns>QueryResult from the Demographics query</returns>
        public async Task<QueryResult> GetDemographics(MapView mapView)
        {
            // demographics layer query
            string demoQueryString = surveyResult.CreateDemoQueryString();
            if (demoQueryString != "1=0")
            {
                try
                {
                    //set up and run query
                    QueryTask demoQueryTask = new QueryTask(
                        new Uri("http://sampleserver1.arcgisonline.com/ArcGIS/rest/services/Demographics/ESRI_Census_USA/MapServer/1"));
                    Query demoQuery = new Query(demoQueryString)
                    {
                        Geometry = mapView.Extent,
                        ReturnGeometry = true,
                        OutSpatialReference = mapView.SpatialReference
                    };
                    var demoResult = await demoQueryTask.ExecuteAsync(demoQuery);
                    return demoResult;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Unable to process the Demographics data. The Server returned the following message: {0}",ex.Message, "Dmographics Error"));
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Queries the Income layer for all the features that meet the user selected criteria
        /// </summary>
        /// <param name="mapView">Main MapView</param>
        /// <returns>QueryResult from the Income query</returns>
        public async Task<Geometry> GetIncome(MapView mapView)
        {
            // income layer query
            string incomeQueryString = surveyResult.CreateIncomeQueryString();
            if (incomeQueryString != "1=0")
            {
                try
                {
                    QueryTask incomeQueryTask = new QueryTask(
                        new Uri("http://services.arcgisonline.com/arcgis/rest/services/Demographics/USA_Median_Household_Income/MapServer/1"));
                    Query incomeQuery = new Query(incomeQueryString)
                    {
                        Geometry = mapView.Extent,
                        ReturnGeometry = true,
                        OutSpatialReference = mapView.SpatialReference
                    };

                    var incomeResult = await incomeQueryTask.ExecuteAsync(incomeQuery);
                    //create a multipart polygon for intersection
                    if (incomeResult.FeatureSet.Features.Count > 0)
                    {
                        return GeometryEngine.Union(incomeResult.FeatureSet.Features.Select(i => i.Geometry));
                    }
                    else
                    {
                        //bogus point 
                        return new MapPointBuilder(0, 0).ToGeometry();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Unable to process the Income data. The Server returned the following message: {0}", ex.Message, "Income Error"));
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Queries the Tapestry layer for all the tapestry polygons that intersect Demo and Income data
        /// </summary>
        /// <param name="mapView"></param>
        /// <param name="geom"></param>
        /// <returns></returns>
        public async Task<QueryResult> GetTapestry(MapView mapView, Geometry geom)
        {
            try { 
                //tapestry layer query
                QueryTask tapestryQueryTask = new QueryTask(
                    new Uri("http://services.arcgisonline.com/arcgis/rest/services/Demographics/USA_Tapestry/MapServer/1"));
                Query tapestryQuery = new Query(geom, SpatialRelationship.Intersects)
                {
                    OutFields = { "DOMTAP", "OBJECTID", "TAPSEGNAM" }, //fields needed to create URLs and labels
                    ReturnGeometry = true,
                    OutSpatialReference = mapView.SpatialReference
                };
                var tapestryResult = await tapestryQueryTask.ExecuteAsync(tapestryQuery);
                return tapestryResult;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Unable to process Tapestry data. The Server returned the following message: {0}", ex.Message, "Tapestry Error"));
                return null;
            }
        }

        // click event for Submit button
        public async void SubmitButtonAction(MapView mapView)
        {
            ProgressOverlayVisibility = Visibility.Visible;
            TapestryLinksVisibility = Visibility.Visible;

            // keep track of already mapped tapestry polygons
            List<int> MappedOIDs = new List<int>();
            TapestryNames = new ObservableCollection<Tapestry>();
            try
            {
                // create graphics overlay and set renderer
                GraphicsOverlay graphicsOverlay = mapView.GraphicsOverlays["graphicsOverlay"];
                SetRenderer(graphicsOverlay);

                if (!(graphicsOverlay.Graphics.Count == 0))
                    graphicsOverlay.Graphics.Clear();

                //get demo and income data
                List<Geometry> PreFinalGeometryList = new List<Geometry>();
                var demoResult = await GetDemographics(mapView);
                var incomePoly = await GetIncome(mapView);

                if (demoResult == null && incomePoly == null) //user has not selected anything
                {
                    MessageBox.Show("Your search returned no results. Try changing your selections");
                    ProgressOverlayVisibility = Visibility.Collapsed;
                    return;
                }
                else if (demoResult == null && incomePoly != null) //if user hasn't selected demographic data
                {
                    PreFinalGeometryList.Add(incomePoly);
                }
                else if (demoResult != null && incomePoly == null) //if user hasn't selected income data
                {
                    PreFinalGeometryList = demoResult.FeatureSet.Features.Select(i => i.Geometry).ToList();
                }
                else //if user has selected at least one demo and one income data
                {
                    foreach (Feature demoFeature in demoResult.FeatureSet.Features)
                    {
                        // intersect demo and income polys
                        Geometry resultPoly = GeometryEngine.Intersection(incomePoly, demoFeature.Geometry);
                        if (!Geometry.IsNullOrEmpty(resultPoly))
                        {
                            PreFinalGeometryList.Add(resultPoly);
                        }
                    }
                    if (PreFinalGeometryList.Count == 0)
                    {
                        MessageBox.Show("Your search returned no results. Try changing your selections");
                        return;
                    }
                }


                // once demographics features are returned, intersect with tapestry layer to get tapestry polygons
                foreach (Geometry geom in PreFinalGeometryList)
                {
                    var tapestryResult = await GetTapestry(mapView, geom);
                    if (tapestryResult != null && tapestryResult.FeatureSet != null)
                    {
                        //remove duplicate features and add to map
                        foreach (Feature tapestryFeature in tapestryResult.FeatureSet.Features)
                        {
                            int oid = (int)(tapestryFeature.Attributes["OBJECTID"]);
                            if (!MappedOIDs.Contains (oid))
                            {
                                graphicsOverlay.Graphics.Add(tapestryFeature as Graphic);
                                MappedOIDs.Add(oid);

                                //add to list of mapped tapestries
                                if (!tapestryNames.Select(i => i.Name).Contains((string)tapestryFeature.Attributes["TAPSEGNAM"]))
                                {
                                    tapestryNames.Add(new Tapestry() { ID = (int)tapestryFeature.Attributes["DOMTAP"], Name = (string)tapestryFeature.Attributes["TAPSEGNAM"] });
                                }
                            }
                        }
                    }
                }
                //hide progress overlay 
                ProgressOverlayVisibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("An error has occured and your information could not be processed: {0}", ex.Message, 
                    "Data Processing Error"));
            }
        }

        /// <summary>
        /// Method sets the same label renderer on the graphics overlay as ESRI's tapestry layer
        /// </summary>
        /// <param name="graphicsOverlay">MapView's graphic layer to display tapestry data</param>
        public async void SetRenderer(GraphicsOverlay graphicsOverlay)
        {
            FeatureLayer layer = new FeatureLayer(
    new Uri("http://services.arcgisonline.com/arcgis/rest/services/Demographics/USA_Tapestry/MapServer/1"));

            await layer.InitializeAsync();
            graphicsOverlay.Renderer = layer.Renderer;
        }

        #region INotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Class defines needed information for each tapestry and generates url
    /// </summary>
    public class Tapestry
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string PdfUrl
        {
            get { return String.Format("http://www.esri.com/~/media/Files/Pdfs/data/esri_data/pdfs/tapestry-singles/segment{0}.pdf", ID); }
        }
    }

    //setting up button command handler
    public class CommandHandler : ICommand
    {
        private Action<MapView> _action;
        private bool _canExecute;
        private MapView _mapView;

        public CommandHandler(Action<MapView> action, bool canExecute)
        {
            _action = action;
            _canExecute = canExecute;
            
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _mapView = parameter as MapView;
            _action(_mapView);
        }
    }

    /// <summary>
    /// Contains all survey properties and handles parsing responses from user
    /// </summary>
    public class SurveyResult
    {
        public string Gender { get; set; }
        public string Age { get; set; }
        public string MaritalStatus { get; set; }
        public string HouseholdSize { get; set; }
        public string HouseholdIncome { get; set; }

        /// <summary>
        /// Creates query string for the income query from user input
        /// </summary>
        /// <returns>Income data query string</returns>
        public string CreateIncomeQueryString()
        {
            StringBuilder qsb = new StringBuilder();

            if (HouseholdIncome != null)
            {
                switch (HouseholdIncome)
                {
                    case "Less than $24,999":
                        qsb.Append("(0  <= MEDHINC_CY AND MEDHINC_CY <= 24999)");
                        break;
                    case "$25,000 - $49,999":
                        qsb.Append("(25000 <= MEDHINC_CY AND MEDHINC_CY <= 49999)");
                        break;
                    case "$50,000 - $74,999":
                        qsb.Append("(50000 <= MEDHINC_CY AND MEDHINC_CY <= 74999)");
                        break;
                    case "$75,000 - $99,000":
                        qsb.Append("(75000 <= MEDHINC_CY AND MEDHINC_CY <= 99000)");
                        break;
                    case "$100,000 - $149,999":
                        qsb.Append("(100000 <= MEDHINC_CY AND MEDHINC_CY <= 149999)");
                        break;
                    case "$150,000 - $199,999":
                        qsb.Append("(150000 <= MEDHINC_CY AND MEDHINC_CY <= 199999)");
                        break;
                    case "$200,000 or greater":
                        qsb.Append("(200000 <= MEDHINC_CY)");
                        break;
                }
            }
            if (qsb.Length == 0)
                return "1=0";
   
            return qsb.ToString();
        }

        /// <summary>
        /// Creates query string for the demographics query from user input
        /// </summary>
        /// <returns>Demo data query string</returns>
        public string CreateDemoQueryString()
        {
            StringBuilder qsb = new StringBuilder();

            // gender 
            if (Gender != null)
            {
                if (qsb.Length > 0)
                    qsb.Append(" and ");

                if (Gender == "Female")
                    qsb.Append("(FEMALES >= MALES)");
                else
                    qsb.Append("(FEMALES <= MALES)");
            }

            // age
            if (Age != null)
            {
                if (qsb.Length > 0)
                    qsb.Append(" and ");

                switch (Age)
                {
                    case "18-21":
                        qsb.Append("(18 <= MED_AGE AND MED_AGE <= 21)"); break;
                    case "22-29":
                        qsb.Append("(22 <= MED_AGE AND MED_AGE <= 29)"); break;
                    case "30-39":
                        qsb.Append("(30 <= MED_AGE AND MED_AGE <= 39)"); break;
                    case "40-49":
                        qsb.Append("(40 <= MED_AGE AND MED_AGE <= 49)"); break;
                    case "50-64":
                        qsb.Append("(50 <= MED_AGE AND MED_AGE <= 64)"); break;
                    case "65 and over":
                        qsb.Append("(65 <= MED_AGE)"); break;
                }
            }

            //marital status
            if (MaritalStatus != null)
            {
                if (qsb.Length > 0)
                    qsb.Append(" and ");

                switch (MaritalStatus)
                {
                    case "Never Married":
                    case "Widowed":
                    case "Divorced":
                        qsb.Append("((MARHH_CHD + MARHH_NO_C) <= (HOUSEHOLDS / 2))");
                        break;
                    case "Married":
                        qsb.Append("((MARHH_CHD + MARHH_NO_C) >= (HOUSEHOLDS / 2))");
                        break;
                }
            }

            //household size
            if (HouseholdSize != null)
            {
                if (qsb.Length > 0)
                    qsb.Append(" and ");

                if (HouseholdSize == "7+")
                    qsb.Append("(AVE_HH_SZ >= 7)");
                else
                    qsb.AppendFormat("(({0} - 1) <= AVE_HH_SZ AND AVE_HH_SZ <= ({0} + 1))", HouseholdSize);
            }

            if (qsb.Length == 0)
                return "1=0";

            return qsb.ToString();
        }
    }
}
