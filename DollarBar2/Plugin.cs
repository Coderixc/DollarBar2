#define TRACE
using System;
using System.Runtime.InteropServices;
using CustomResolutionsTypes;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Data;
using System.Reflection;
using System.Collections.Generic;

namespace DollarBar2
{
	[ComVisible(true)]
	[Guid("ba0b2a99-9014-4057-9973-66b2ea0ff4b3")]
	[ClassInterface(ClassInterfaceType.None)]
	[CustomResolutionPluginAttribute(RuleOHLC = true)]
	public class Plugin : ICustomResolutionPlugin, ICustomPluginFormatParams, ICustomResolutionStyles
	{
		#region Declare Variable
		private bool flag_TickblazeOutputwindow = false;

		private Indiactaor LibIndicator;


		private List<double> ListPrice_minutes = new List<double>();
		private List<double> ListVolume_minutes = new List<double>();

		private Queue<double> QueuePrice_mean;
		private Queue<double> QueueVolume_sum;
		private List<double> list_barsizevar = new List<double>();


		private double _barSize = 0;
		private double _barSizeVar = 0;

		private int PreviousDate = 0; //Past
		private int ProcessDate = 0; //Presnt
		private bool Flag_SingleUse = true;
		private double barSizeFix;

		#endregion
		#region Ctor
		public Plugin()
		{
			this.QueuePrice_mean = new Queue<double>();
			this.QueueVolume_sum = new Queue<double>();
			this.LibIndicator = new Indiactaor();

			//*** For now we have changed this to a default value of 5 Billion. If you Open BTCUSD with Bitfinex Data or XBTUSD - the data we send u in the email, you can see that bar is being sampled every time volume crosses 5,000,000,000
			this.barSizeFix = 5000000;//5000000000;  //TODO : Load Default bar size while Loading Format Instruments..


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
		private double m_PointValue = 0.0001;
		private long m_MinMovement = 1;
		#endregion

		public void Init(IBaseOptions baseOptions, IParams customParams)
		{
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
			this.ListPrice_minutes.Add(close);
			this.ListVolume_minutes.Add(volumeAdded);

			//*** Have you checked the format of time_in_ticks ?
			//635726880600000000-- > 20150717000100
			//635726880600000000-- > 20150717000100
			//635726881200000000---> 20150717000200
			//635726881800000000---> 20150717000300



			string dt = DateTimeString(time_in_ticks);
			if (this.Flag_SingleUse)
			{
				this.PreviousDate = Convert.ToInt32(dt.Substring(0, 8));
				this.Flag_SingleUse = false;
			}

			this.ProcessDate = Convert.ToInt32(dt.Substring(0, 8));


			#region Resampling data for a Day in Multicharts
			if (this.PreviousDate != this.ProcessDate)
			{

				//Creating List1 which holds the closing prices.mean()
				double res = this.resample_WithMean(this.ListPrice_minutes);
				//this.ListPrice_mean.Add(res);
				this.QueuePrice_mean.Enqueue(res);

				//Creating List2 which holds the volumes.sum()
				double vol = this.resample_WithoutMean(this.ListVolume_minutes);
				//this.ListVolume_sum.Add(vol);
				this.QueueVolume_sum.Enqueue(vol);

				//this.minute_dollarvalue.Clear(); // Clear List when Resmapling is Done for a day
				this.ListPrice_minutes.Clear();
				this.ListVolume_minutes.Clear();

				this.PreviousDate = Convert.ToInt32(dt.Substring(0, 8));
			}
			#endregion

			int Threshold = Math.Max(this.QueuePrice_mean.Count, this.QueueVolume_sum.Count);
			if (Threshold >= 30)
			{
				this.list_barsizevar = this.LibIndicator.Multiply(this.QueuePrice_mean.ToList(), this.QueueVolume_sum.ToList());

				// Remove first elements
				this.QueuePrice_mean.Dequeue();
				this.QueueVolume_sum.Dequeue();

				this._barSizeVar = this.LibIndicator.Simple_MovingAverage(this.list_barsizevar, 30) / 50;

				this._barSize = _barSizeVar;

			}
			else
			{
				this._barSize = this.barSizeFix; //By Default : TODO Link with variable similar to Tick Blaze
			}

			m_Volume += volumeAdded;
			m_UpVolume += upVolumeAdded;
			m_DownVolume += downVolumeAdded;

			Bar.UpdateBar(time_in_ticks, tickId, open, high, low, close, m_Volume, m_UpVolume, m_DownVolume, trend, true, false);

			if (isBarClose)
			{
				if (m_UpVolume >= this._barSize)
				{
					Bar.CloseBar();
					m_Volume = 0;
					m_UpVolume = 0;
					m_DownVolume = 0;

				}
			}


			//if (isBarClose)
			//{

			//	//if (m_UpVolume >= this._barSize)
			//	if (m_UpVolume >= this._barSize)
			//	{

			//		Bar.UpdateBar(time_in_ticks, tickId, open, high, low, close, m_Volume, m_UpVolume, m_DownVolume, trend, true, false);

			//		//Bar.CloseBar();
			//		m_Volume = 0;
			//		m_UpVolume = 0;
			//		m_DownVolume = 0;


			//		//logic similar to TB simulation
			//		//BarBuilderSetIsMissing(bool)
			//		//Bar.CloseBar();

			//		// Set the start Datetime
			//		//BarBuilderSetStartDateTime(starttime);
			//		//Format of UPDATEBar is Different  from Tick blazw, no need  update Starttime function
			//		//as  have used logic similar to above 
			//		//this.ProcessDate = Convert.ToInt32(dt.Substring(0, 8));

			//		// Set the end Datetime
			//		//endtime = endDateTime;
			//		//BarBuilderSetEndDateTime(endtime);



			//		// Set the Close
			//		//BarBuilderSetClose(close);

			//		//Set the High 
			//		//BarBuilderSetHigh(highest_high);

			//		// Set the Low
			//		//BarBuilderSetLow(lowest_low);

			//		// Set the Open
			//		//open_of_first_bar_ = Convert.ToDouble(open_of_first_bar);
			//		//BarBuilderSetOpen(open_of_first_bar_);



			//	}
			//	Bar.CloseBar();
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

		private EStyleType[] m_Styles = new EStyleType[] { EStyleType.Candlestick,EStyleType.OHLC,EStyleType.HollowCandlestick};
		#endregion

		#region Date EPOCH Time TO String
		private string DateTimeString(long epochtime)
		{
			string result = String.Empty;

			try
			{
				String dt = new DateTime(epochtime).ToString("yyyyMMddHHmmss");
				return dt;
			}
			catch (Exception ex)
			{

				return result;
			}



		}
		#endregion

		#region Calculating Mean
		private double mean(double totalsum, int noofsample)
		{
			if (flag_TickblazeOutputwindow)
			{
				MessageBox.Show("Entered into mean(Sum) Method");
			}
			double result = 0.0;
			if (noofsample <= 0)
				return -1.0;
			try
			{
				result = totalsum / noofsample;
				return result;

			}
			catch (Exception ex)
			{
				MessageBox.Show("Unhandled Exception in ExtractDateonly " + ex.ToString());
				result = 0.0;
				return result;
			}
		}
		#endregion

		#region Applying Resample
		private double resample_WithMean(List<double> List_Input, int freq = 1)
		{
			if (flag_TickblazeOutputwindow)
			{
				MessageBox.Show("Entered into Resampling  With Mean Method");
			}
			double sum = 0.0;
			double result = 0.0;
			int i = 0;
			try
			{
				for (i = 0; i < List_Input.Count; i++)
				{
					sum = sum + List_Input[i];

				}
				result = mean(sum, List_Input.Count);
				return result;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				return 0.0;
			}
		}
		#endregion

		#region Resampling Without Mean
		private double resample_WithoutMean(List<double> List_Input, int freq = 1)
		{
			if (flag_TickblazeOutputwindow)
			{
				MessageBox.Show("Entered into Resampling  Without Mean Method");
			}
			double sum = 0.0;
			double result = 0.0;
			int i = 0;
			try
			{
				for (i = 0; i < List_Input.Count; i++)
				{
					result = result + List_Input[i];

				}
				;
				return result;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				return 0.0;
			}
		}
		#endregion

		#region  Indicator Function
		class Indiactaor
		{
			private List<double> LitsResult;

			public Indiactaor()
			{

				this.LitsResult = new List<double>();
			}


			#region Simple Moving average
			public double Simple_MovingAverage(List<double> listBar, int barback)
			{
				if (barback == 0)
					return 0; //Can't divide by 0

				double result = 0.0;
				int loopend = listBar.Count - barback;
				int idx = 0;
				string element = "";
				try
				{
					if (loopend < 0)
					{
						//return  TODO : Pending 
					}

					for (idx = listBar.Count - 1; idx > loopend - 1; idx--)
					{
						result += listBar[idx];
						element = element + listBar[idx] + ",";
					}
				}
				catch (Exception e)
				{
					MessageBox.Show(" Erorr(s) Ocuured in " + MethodInfo.GetCurrentMethod().Name + $"{e.Message }");

				}

				return (double)result / barback;
			}
			#endregion

			#region Multiply Two list
			public List<double> Multiply(List<double> list1, List<double> list2)
			{
				try
				{
					this.LitsResult.Clear();
					int Max = Math.Max(list1.Count, list2.Count);

					for (int i = 0; i < Max; i++)
					{
						try
						{

							double result = 0;

							if (list1.ElementAtOrDefault(i) == 0.0)
							{
								list1.Add(0.0);
							}
							if (list2.ElementAtOrDefault(i) == 0.0)
							{
								list2.Add(0.0);
							}

							result = list1.ElementAt(i) * list2.ElementAt(2);

							this.LitsResult.Add(result);


						}
						catch (Exception e)
						{

						}
					}

					return this.LitsResult;

				}
				catch (Exception ex)
				{
					string err = $"Error(s) Occured in {MethodInfo.GetCurrentMethod().Name} error message {ex.Message}  ";
					return this.LitsResult;
				}


			}

			#endregion
		}
		#endregion
	}
}