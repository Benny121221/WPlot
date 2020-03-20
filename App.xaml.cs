﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Where1.WPlot
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private List<PlotParameters> series = new List<PlotParameters>();

		public List<PlotParameters> GetSeries() => series;
		public void ClearSeries() => series = new List<PlotParameters>();

		public void AddSeries(PlotParameters plotParams)
		{
			series.Add(plotParams);
			((MainWindow)this.MainWindow).RenderPlot();
		}

		public PlotParameters AddSeriesFromString(string dataString, DrawSettings drawSettings, Dictionary<string, object> metadata = null)
		{
			object data = new object();

			string[] raw = dataString.Split(new char[] { ',', '\n' });

			if (!drawSettings.dateXAxis)
			{
				List<double> serialData = raw.Where(m => double.TryParse(m, out _)).Select(m => double.Parse(m)).ToList();

				if (drawSettings.type == PlotType.scatter || drawSettings.type == PlotType.bar)
				{
					double[] xs = new double[serialData.Count / 2];
					double[] ys = new double[serialData.Count / 2];
					for (int i = 0; i < serialData.Count; i++)
					{
						int row = i / 2;
						int col = i % 2;

						if (col == 0)
						{
							xs[row] = serialData[i];
						}
						else
						{
							ys[row] = serialData[i];
						}

						data = new double[][] { xs, ys };
					}
				}
				else if (drawSettings.type == PlotType.signal || drawSettings.type == PlotType.histogram)
				{
					data = serialData.ToArray();
				}

				if (drawSettings.type == PlotType.scatter && drawSettings.polarCoordinates)
				{
					(((double[][])data)[0], ((double[][])data)[1]) = ScottPlot.Tools.ConvertPolarCoordinates(((double[][])data)[0], ((double[][])data)[1]);
				}
			}
			else {
				List<DateTime> dataX = new List<DateTime>();
				List<double> dataY = new List<double>();

				int successfullyParsed = 0;
				for (int i = 0; i < raw.Length; i++) {
					if (successfullyParsed % 2 == 0)
					{
						DateTime temp;
						if (DateTime.TryParse(raw[i], out temp))
						{
							dataX.Add(temp);
							successfullyParsed++;
						}
					}
					else {
						double temp;
						if (double.TryParse(raw[i], out temp)) {
							dataY.Add(temp);
							successfullyParsed++;
						}
					}
				}

				long totalDistanceTicks=0;
				for (int i = 0; i < dataX.Count; i++) {
					if (i == dataX.Count - 1) {
						break;
					}

					totalDistanceTicks += Math.Abs(dataX[i].Ticks - dataX[i + 1].Ticks);
				}
				long averageTickDistance = totalDistanceTicks / (dataX.Count - dataX.Count % 2); //The fact that this is rounded doesn't matter, bc ticks are so obsenely small
				long averageSecondsDistance = averageTickDistance / 10_000_000;

				if (averageSecondsDistance > 0.75 * 86400 * 365)
				{
					metadata["timeUnit"] = ScottPlot.Config.DateTimeUnit.Year;
				}
				else if (averageSecondsDistance > 0.75 * 86400 * 30)
				{
					metadata["timeUnit"] = ScottPlot.Config.DateTimeUnit.Month;
				}
				else if (averageSecondsDistance > 0.75 * 86400)
				{
					metadata["timeUnit"] = ScottPlot.Config.DateTimeUnit.Day;
				}
				else if (averageSecondsDistance > 0.75 * 3600) { 
					metadata["timeUnit"] = ScottPlot.Config.DateTimeUnit.Hour;
				}
				else if (averageSecondsDistance > 0.75 * 60)
				{
					metadata["timeUnit"] = ScottPlot.Config.DateTimeUnit.Minute;
				}
				else
				{
					metadata["timeUnit"] = ScottPlot.Config.DateTimeUnit.Second;
				}
				metadata["startDate"] = dataX[0];

				double[] dataXDouble = dataX.Select((dt, i)=> dt.ToOADate()).ToArray();


				data = new double[][] { dataXDouble, dataY.ToArray() };
			}

			PlotParameters plotParams = new PlotParameters { data = data, drawSettings = drawSettings, metaData = metadata };
			series.Add(plotParams);

			((MainWindow)this.MainWindow).RenderPlot();

			return plotParams;
		}

		public PlotParameters AddSeriesFromCSVFile(string path, DrawSettings drawSettings, Dictionary<string, object> metadata = null)
		{
			using (StreamReader file = new StreamReader(path))
			{
				string raw = file.ReadToEnd();
				return AddSeriesFromString(raw, drawSettings, metadata);
			}
		}

		public void AddErrorFromCSVFile(PlotParameters plotParams, string path)
		{
			object data = new object();

			using (StreamReader file = new StreamReader(path))
			{
				string[] raw = file.ReadToEnd().Split(new char[] { ',', '\n' });
				List<double> serialData = raw.Where(m => double.TryParse(m, out _)).Select(m => double.Parse(m)).ToList();

				if (plotParams.drawSettings.type == PlotType.scatter)
				{
					double[] xs = new double[serialData.Count / 2];
					double[] ys = new double[serialData.Count / 2];
					for (int i = 0; i < serialData.Count; i++)
					{
						int row = i / 2;
						int col = i % 2;

						if (col == 0)
						{
							xs[row] = serialData[i];
						}
						else
						{
							ys[row] = serialData[i];
						}

						data = new double[][] { xs, ys };
					}
				}
				else if (plotParams.drawSettings.type == PlotType.bar)
				{
					data = serialData.ToArray();
				}

				plotParams.errorData = data;
				plotParams.hasErrorData = true;
			}

			((MainWindow)this.MainWindow).RenderPlot();
		}
	}
}
