#define TRACE
using System;
using System.Runtime.InteropServices;
using CustomResolutionsTypes;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;

namespace DollarBar2
{
	[ComVisible(true)]
	[Guid("ba0b2a99-9014-4057-9973-66b2ea0ff4b3")]
	[ClassInterface(ClassInterfaceType.None)]
	[CustomResolutionPluginAttribute(RuleOHLC=true)]
	public class Plugin : ICustomResolutionPlugin, ICustomPluginFormatParams, ICustomResolutionStyles
	{
		#region Declare Variable
		ICustomBar Bar;
		FilePrint ExportData;

		private int PreviousDate = 0; //Past
		private int ProcessDate = 0; //Presnt
		private bool Flag_SameDate = true;

		#endregion


		#region Ctor
		public Plugin()
		{
			int a = 10;
			int b = 15;

			//IBaseOptions baseOptions  = new Base64FormattingOptions();

			try
			{
				this.ExportData = new FilePrint();
			}
			catch
			{ 
			
			}


			//MessageBox.Show("Construtor Called");

			Trace.Write("Hello World");

		}
		#endregion

		#region ICustomResolutionPlugin
		public String Name
		{
			get
			{
				return "DollarBar2";
			}
		}

		public String Guid
		{
			get
			{
				return "4fb4291c-42f2-475b-be60-07065caddac7";
			}
		}

		public String Description
		{
			get
			{
				return "";
			}
		}

		public String Vendor
		{
			get
			{
				return "abc";
			}
		}

		#region Properties
		private long m_Volume = 0;
		private long m_UpVolume = 0;
		private long m_DownVolume = 0;
		private double	m_PointValue = 0.0001;
		private long m_MinMovement = 1;
		#endregion

		public void Init(IBaseOptions baseOptions, IParams customParams)
		{

			//MessageBox.Show("INIT");
			if (baseOptions != null)
			{
				m_PointValue = baseOptions.PointValue;
				m_MinMovement = baseOptions.MinMovement;
			}

			Trace.TraceInformation(string.Format("Init {0}: PointValue={1}, MinMovement={2}",
				ToString(), m_PointValue, m_MinMovement));
		}

		public void OnData(ICustomBar Bar, Int64 time_in_ticks, Int32 tickId, double open, double high, double low, double close, long volumeAdded, long upVolumeAdded, long downVolumeAdded, ECustomBarTrendType trend, bool isBarClose)
		{


			string dt = DateTimeString(time_in_ticks);
			if(Flag_SameDate)
            {
				this.PreviousDate =Convert.ToInt32( dt.Substring(0,8));
				this.Flag_SameDate  = false;
			}

			this.ProcessDate = Convert.ToInt32(dt.Substring(0, 8));

			if(this.PreviousDate != this.ProcessDate)
            {

            }




			String data = Bar + ","
					+ time_in_ticks + ","
					+ tickId + ","
					+ open + ","
					+ high + ","
					+ low + ","
					+ close + ","
					+ volumeAdded + ","
					+ upVolumeAdded + ","
					+ downVolumeAdded + ","
					+ trend + ","
					+ isBarClose;

			try
			{
				File.WriteAllText(this.ExportData.Datapath, data);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}




			m_Volume += volumeAdded;
			m_UpVolume += upVolumeAdded;
			m_DownVolume += downVolumeAdded;
			Bar.UpdateBar(time_in_ticks, tickId, open, high, low, close, m_Volume, m_UpVolume, m_DownVolume, trend, true, false);

			if (isBarClose)
			{
				if (m_UpVolume >= 100000)
				{
					Bar.CloseBar();
					m_Volume = 0;
					m_UpVolume = 0;
					m_DownVolume = 0;

				}

			}

			//m_Volume	+= volumeAdded;
			//m_UpVolume	+= upVolumeAdded;
			//m_DownVolume	+= downVolumeAdded;

			//Bar.UpdateBar(time_in_ticks, tickId, open, high, low, close, m_Volume, m_UpVolume, m_DownVolume, trend, true, false);
			//if (isBarClose)
			//{
			//	Bar.CloseBar();
			//	m_Volume = 0;
			//	m_UpVolume = 0;
			//	m_DownVolume = 0;
			//}
		}

		public void Reset()
		{
			m_Volume = 0;
			m_UpVolume = 0;
			m_DownVolume = 0;
		}
		#endregion

		#region ICustomPluginFormatParams
		public void FormatParams(IParams customParams, IPriceScale priceScale, out string formattedParams)
        {
            formattedParams = Name;
        }
		#endregion

		#region ICustomResolutionStyles
		public Int32 StyleCount
		{
			get
			{
				return m_Styles.Length;
			}
		}
		public EStyleType GetStyle(Int32 Idx)
		{
			return m_Styles[Idx];
		}

		private EStyleType[] m_Styles = new EStyleType[] { EStyleType.OHLC };
        #endregion

        #region Date EPOCH Time TO String
		private string DateTimeString(long epochtime)
        {
			string result = String.Empty;
			
            try
            {
				String dt = new DateTime(epochtime).ToString("yyyyMMddHHmmss");
				return  dt;	
			}
			catch (Exception ex)
            {

				return result;
            }



		}
        #endregion

    }




    class FilePrint
    {

		static string st = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss").ToString();
		string path = @"C:\Users\kchan\OneDrive\Desktop\TSOUTPUT";
		public string Datapath = @"C:\Users\kchan\OneDrive\Desktop\TSOUTPUT\BTCUSD_"+ st + ".csv";

		private bool Sinleuse = true;


		//public const string OHLC_CSVpath = @".\\1INCHUSD.csv";
		//public const string BTCUSD_CSVpath = @".\\BTCUSD.txt";

		public FilePrint()
        {
            if (!Directory.Exists(path))
            {

                Directory.CreateDirectory(path);


            }
            try
            {

                if (File.Exists(Datapath))
                {
                    File.Delete(Datapath);

                    //File.SetAttributes(Datapath, FileAttributes.Normal);
                    //File.Delete(Datapath);

                }
               // File.Create(Datapath);

				var myFile = File.Create(Datapath);
				//myPath = "C:\file.txt"
				myFile.Close();

				//File.Copy(file, dest, true);
				//File.SetAttributes(dest, FileAttributes.Normal);
			}
            catch (Exception ex)
            {

            }

            //create coloumn

            if (Sinleuse)
            {
				string header =
						 "Bar" + ","
						+ "time_in_ticks " + ","
						+ "tickId " + ","
						+ "open " + ","
						+ "high " + ","
						+ "low " + ","
						+ "close " + ","
						+ "volumeAdded " + ","
						+ "upVolumeAdded " + ","
						+ "downVolumeAdded " + ","
						+ "trend " + ","
						+ "isBarClose";

				File.WriteAllText(Datapath, header);
				Sinleuse = false;

			}

		}


		public void OnDataReceiveData(ICustomBar Bar, Int64 time_in_ticks, Int32 tickId, double open, double high, double low, double close, long volumeAdded, long upVolumeAdded, long downVolumeAdded, ECustomBarTrendType trend, bool isBarClose)
        {
			String data = Bar + ","
						+ time_in_ticks + ","
						+ tickId + ","
						+ open + ","
						+ high + ","
						+ low + ","
						+ close + ","
						+ volumeAdded + ","
						+ upVolumeAdded + ","
						+ downVolumeAdded + ","
						+ trend + ","
						+ isBarClose;

            try
            {
				File.WriteAllText(Datapath, data);
			}
			catch (Exception ex)
            {
				MessageBox.Show(ex.Message);
            }


		}

    }

}

