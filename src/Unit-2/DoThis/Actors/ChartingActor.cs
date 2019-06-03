using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ChartingActor : ReceiveActor
    {
        /// <summary>
        /// Maximum number of points we allow in a series.
        /// </summary>
        public const int MaxPoints = 250;

        /// <summary>
        /// Incrementing the counter we use to plot along the x-axis.
        /// </summary>
        private int _xPosCounter;
        
        #region Messages

        public class InitializeChart
        {
            public InitializeChart(Dictionary<string, Series> initialSeries)
            {
                InitialSeries = initialSeries;
            }

            public Dictionary<string, Series> InitialSeries { get; }
        }
        
        /// <summary>
        /// Add a new <see cref="Series"/> to the chart.
        /// </summary>
        public class AddSeries
        {
            public AddSeries(Series series)
            {
                Series = series;
            } 
            
            public Series Series { get; }
        }

        /// <summary>
        /// Remove an existing <see cref="Series"/> from the chart.
        /// </summary>
        public class RemoveSeries
        {
            public RemoveSeries(string seriesName)
            {
                SeriesName = seriesName;
            }
            
            public string SeriesName { get; }
        }
        
        /// <summary>
        /// Toggles pausing updates to chart.
        /// </summary>
        public class TogglePause { }
        
        #endregion

        private readonly Chart _chart;
        private readonly Button _pauseButton;
        private Dictionary<string, Series> _seriesIndex;

        public ChartingActor(Chart chart, Button pauseButton) 
            : this(chart, new Dictionary<string, Series>(), pauseButton)
        {
        }

        public ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex, Button pauseButton)
        {
            _chart = chart;
            _seriesIndex = seriesIndex;
            _pauseButton = pauseButton;
            Charting(); // Enter the initial behavior (state)
        }
        
        #region Behaviors

        private void Charting()
        {
            Receive<InitializeChart>(ic => HandleInitialize(ic));
            Receive<AddSeries>(addSeries => HandleAddSeries(addSeries));
            Receive<RemoveSeries>(removeSeries => HandleRemoveSeries(removeSeries));
            Receive<Metric>(metric => HandleMetrics(metric));
            
            // New receive handler for `TogglePause` message type
            Receive<TogglePause>(pause =>
            {
                SetPauseButtonText(true);
                BecomeStacked(Paused);
            });
        }

        private void Paused()
        {
            Receive<Metric>(metric => HandleMetricsPaused(metric));
            Receive<TogglePause>(pause =>
            {
                SetPauseButtonText(false);
                UnbecomeStacked();
            });
        }
        
        #endregion

        #region Individual Message Type Handlers

        private void HandleInitialize(InitializeChart ic)
        {
            if (ic.InitialSeries != null)
            {
                // Swap the two series out.
                _seriesIndex = ic.InitialSeries;
            }
            
            // Delete any existing series.
            _chart.Series.Clear();
            
            // Set up the axes.
            var area = _chart.ChartAreas[0];
            area.AxisX.IntervalType = DateTimeIntervalType.Number;
            area.AxisY.IntervalType = DateTimeIntervalType.Number;
            
            SetChartBoundaries();
            
            // Try to render the initial chart. This **may** change the chart boundaries.
            if (_seriesIndex.Any())
            {
                foreach (var series in _seriesIndex)
                {
                    // Force chart and internal index to use the same names.
                    series.Value.Name = series.Key;
                    _chart.Series.Add(series.Value);
                }
            }
            
            SetChartBoundaries();
        }

        private void HandleAddSeries(AddSeries series)
        {
            if (!string.IsNullOrEmpty(series.Series.Name) && !_seriesIndex.ContainsKey(series.Series.Name))
            {
                _seriesIndex.Add(series.Series.Name, series.Series);
                _chart.Series.Add(series.Series);
                SetChartBoundaries();
            }
        }

        private void HandleRemoveSeries(RemoveSeries series)
        {
            if (!string.IsNullOrEmpty(series.SeriesName) && _seriesIndex.ContainsKey(series.SeriesName))
            {
                var seriesToRemove = _seriesIndex[series.SeriesName];
                _seriesIndex.Remove(series.SeriesName);
                _chart.Series.Remove(seriesToRemove);
                SetChartBoundaries();
            }
        }

        private void HandleMetrics(Metric metric)
        {
            if (!string.IsNullOrEmpty(metric.Series) && _seriesIndex.ContainsKey(metric.Series))
            {
                var series = _seriesIndex[metric.Series];
                
                // If we are shutting down
                if (series.Points == null)
                {
                    return;
                }
                
                series.Points.AddXY(_xPosCounter++, metric.CounterValue);
                while (series.Points.Count > MaxPoints)
                {
                    series.Points.RemoveAt(0);
                }
                SetChartBoundaries();
            }
        }

        private void HandleMetricsPaused(Metric metric)
        {
            if (!string.IsNullOrEmpty(metric.Series) && _seriesIndex.ContainsKey(metric.Series))
            {
                var series = _seriesIndex[metric.Series];
                
                // If we are shutting down
                if (series.Points == null)
                {
                    return;
                }
                
                // Set the Y-value to zero when were paused
                series.Points.AddXY(_xPosCounter++, 0.0d);
                while (series.Points.Count > MaxPoints)
                {
                    series.Points.RemoveAt(0);
                }
                SetChartBoundaries();
            }
        }

        #endregion

        /// <summary>
        /// Manages the boundaries of our chart; specifically, ensures that our chart boundaries are updated when
        /// we remove old points from the beginning of the chart as time elapses.
        /// </summary>
        /// <remarks>
        /// This code has nothing to do with actors; it is UI-management code.
        /// </remarks>
        private void SetChartBoundaries()
        {
            var allPoints = _seriesIndex.Values.SelectMany(series => series.Points).ToList();
            var yValues = allPoints.SelectMany(point => point.YValues).ToList();
            var maxAxisX = _xPosCounter;
            var minAxisX = _xPosCounter - MaxPoints;
            var maxAxisY = yValues.Count > 0 ? Math.Ceiling(yValues.Max()) : 1.0d;
            var minAxisY = yValues.Count > 0 ? Math.Floor(yValues.Min()) : 0.0d;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (minAxisY == maxAxisY)
            {
                maxAxisY++;
            }

            var area = _chart.ChartAreas[0];
            area.AxisY.Minimum = minAxisY;
            area.AxisY.Maximum = Math.Max(1.0d, maxAxisY);
                
            if (allPoints.Count > 2)
            {
                area.AxisX.Minimum = minAxisX;
                area.AxisX.Maximum = maxAxisX;
            }
        }

        private void SetPauseButtonText(bool isPaused)
        {
            _pauseButton.Text = $@"{(!isPaused ? "PAUSE ||" : "RESUME ->")}";
        }

    }
}
