﻿using CPU_Scheduler.Utils;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Process = CPU_Scheduler.Models.Process;

namespace CPU_Scheduler
{
    public partial class Form1 : Form
    {
        private ComboBox algorithmComboBox;
        private NumericUpDown quantumInput;
        private Label quantumLabel;
        private DataGridView processGrid;
        private Button calculateButton;
        private Button importButton;
        private Button exportButton;
        private DataGridView resultGrid;
        private Label avgWaitingLabel;
        private Label avgTurnaroundLabel;
        private Label bestAlgorithmLabel;
        private Label avgBestAlgorithmLabel;
        private Panel drawPanel;
        private List<Models.Process> result;
        private RadioButton preemptiveRadio;
        private RadioButton nonPreemptiveRadio;
        private GroupBox groupBoxInput;
        private GroupBox groupBoxResult;
        private GroupBox groupAnalysis;
        private GroupBox groupAnalysis1;
        private GroupBox groupAnalysis2;
        private GroupBox groupAnalysis3;
        private Panel simulationPanel;
        private Panel piePanel;
        private Panel histogramPanel;
        private Timer tt = new Timer();
        private int CurrentTime = 0;
        private List<Panel> animatedProcesses = new List<Panel>();
        private int MaxSimulationProgress;
        private List<Models.Process> lastSummary;
        private Dictionary<string, double> avgWaitingTimes = new Dictionary<string, double>();

        public Form1()
        {
            InitializeComponent();
            InitForm();
            InitControls();
            this.Resize += (_, __) => {
                RepositionLayout();
                drawPanel.Invalidate();
                piePanel.Invalidate();
            };
            tt.Tick += Tt_Tick;
        }

        private void Tt_Tick(object sender, EventArgs e)
        {
            CurrentTime++;
            drawPanel.Invalidate();
            piePanel.Invalidate();
            int totalTime = result.Sum(p => p.BurstTime);
            if (totalTime <= CurrentTime)
            {
                CurrentTime = 0;
                tt.Stop();
            }
        }

        private void InitForm()
        {
            this.Text = "CPU Scheduling Simulator";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.WhiteSmoke;
        }

        private void InitControls()
        {
            MaxSimulationProgress = (this.ClientSize.Width - 20) / 3 - 160;
            drawPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            drawPanel.Paint += DrawPanel_Paint;
            groupBoxInput = new GroupBox
            {
                Text = "Process Input",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.Gainsboro,
                Width = this.ClientSize.Width,
                Height = 260,
                Location = new Point(10, 10)
            };

            groupBoxResult = new GroupBox
            {
                Text = "Results",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.Gainsboro,
                Width = this.ClientSize.Width / 2,
                Height = 260,
                Location = new Point(10, 10)
            };

            groupAnalysis = new GroupBox
            {
                Text = "Analysis",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.Gainsboro,
                Width = this.ClientSize.Width - 20,
                Height = 320,
                Location = new Point(10, groupBoxInput.Location.Y + 10)
            };

            groupAnalysis1 = new GroupBox
            {
                Text = "Simulation",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.WhiteSmoke,
                Width = (this.ClientSize.Width - 20) / 3 - 40,
                Height = 275,
                Location = new Point(10, groupBoxInput.Location.Y + 10)
            };

            groupAnalysis2 = new GroupBox
            {
                Text = "Pie Chart",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.WhiteSmoke,
                Width = (this.ClientSize.Width - 20) / 3 - 40,
                Height = 275,
                Location = new Point(20 + (this.ClientSize.Width - 20) / 3 - 40, groupBoxInput.Location.Y + 10)
            };

            groupAnalysis3 = new GroupBox
            {
                Text = "Histogram for All Avg Waiting Time Algorithms",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.WhiteSmoke,
                Width = (this.ClientSize.Width - 20) / 3 - 40,
                Height = 275,
                Location = new Point(30 + ((this.ClientSize.Width - 20) / 3 - 40) * 2, groupBoxInput.Location.Y + 10)
            };

            algorithmComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White,
                ForeColor = Color.Black
            };
            algorithmComboBox.Items.AddRange(new string[] {
                "First Come First Serve (FCFS)",
                "Shortest Job First (SJF)",
                "Priority Scheduling",
                "Round Robin"
            });

            quantumLabel = new Label
            {
                Text = "Quantum:",
                AutoSize = true,
                Visible = false,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            quantumInput = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10,
                Value = 2,
                Visible = false,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White
            };

            processGrid = new DataGridView
            {
                AllowUserToAddRows = true,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                Font = new Font("Segoe UI", 10)
            };
            processGrid.Columns.Add("ProcessID", "Process ID");
            processGrid.Columns.Add("ArrivalTime", "Arrival Time");
            processGrid.Columns.Add("BurstTime", "Burst Time");
            processGrid.Columns.Add("Priority", "Priority");

            calculateButton = new Button { Text = "Calculate", BackColor = Color.ForestGreen, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Height = 40 };
            calculateButton.Click += CalculateButton_Click;

            importButton = new Button { Text = "Import File", BackColor = Color.BlueViolet, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Height = 40 };
            importButton.Click += ImportButton_Click;

            exportButton = new Button { Text = "Export Result", BackColor = Color.MediumBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Height = 40 };
            exportButton.Click += ExportButton_Click;

            resultGrid = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                Font = new Font("Segoe UI", 10)
            };
            resultGrid.Columns.Add("Process", "Process");
            resultGrid.Columns.Add("WaitingTime", "Waiting");
            resultGrid.Columns.Add("TurnaroundTime", "Turnaround");
            resultGrid.Columns.Add("Burst", "Burst");
            resultGrid.Columns.Add("Arrival", "Arrival");

            avgWaitingLabel = new Label { Width = 300, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Black, BackColor = Color.WhiteSmoke };
            avgTurnaroundLabel = new Label { Width = 300, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Black, BackColor = Color.WhiteSmoke };
            bestAlgorithmLabel = new Label { Width = 300, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Black, BackColor = Color.WhiteSmoke };
            avgBestAlgorithmLabel = new Label { Width = 300, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Black, BackColor = Color.WhiteSmoke };

            preemptiveRadio = new RadioButton
            {
                Text = "Preemptive",
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.DarkGreen,
                Visible = false
            };

            nonPreemptiveRadio = new RadioButton
            {
                Text = "Non-Preemptive",
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.Maroon,
                Visible = false,
                Checked = true
            };

            simulationPanel = new Panel
            {
                Name = "simulationPanel",
                AutoScroll = true,
                Width = (this.ClientSize.Width - 60) / 3,
                Height = 255,
                Location = new Point(0, 20),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke
            };

            piePanel = new Panel
            {
                Name = "piePanel",
                Width = groupAnalysis2.Width,
                Height = groupAnalysis2.Height - 20,
                Location = new Point(0, 20),
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle
            };
            piePanel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                if (piePanel.Width >= piePanel.Height)
                {
                    int x = (piePanel.Width - piePanel.Height) / 2;
                    Rectangle pieArea = new Rectangle(x, 10, piePanel.Height - 20, piePanel.Height - 20);
                    DrawPieChart(g, pieArea, lastSummary);
                }
                else
                {
                    Rectangle pieArea = new Rectangle(10, 10, piePanel.Width - 20, piePanel.Width - 20);
                    DrawPieChart(g, pieArea, lastSummary);
                }
            };

            histogramPanel = new Panel
            {
                Name = "histogramPanel",
                Width = groupAnalysis2.Width,
                Height = groupAnalysis2.Height - 20,
                Location = new Point(0, 20),
                BackColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle
            };
            histogramPanel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                Font labelFont = new Font("Segoe UI", 8);
                Brush textBrush = Brushes.Black;
                Brush barBrush = Brushes.SteelBlue;

                int barWidth = 35;
                int spacing = 35;
                int chartHeight = histogramPanel.Height - (spacing * 2 + 10);
                int startX = (histogramPanel.Width - (barWidth * 5 + spacing * 6)) / 2; ;
                int baseY = chartHeight + spacing;


                avgWaitingTimes.Clear();

                List<Process> Clone(List<Process> input) =>
                    input.Select(p => new Process(p.Name, p.ArrivalTime, p.BurstTime, p.Priority)).ToList();

                try
                {
                    List<Models.Process> processes = new List<Models.Process>();
                    processes.Clear();
                    string selectedAlgorithm = "FCFS";
                    try
                    {
                        foreach (DataGridViewRow row in processGrid.Rows)
                        {
                            if (row.IsNewRow) continue;

                            string id = row.Cells["ProcessID"].Value?.ToString();
                            int arrival = int.Parse(row.Cells["ArrivalTime"].Value?.ToString() ?? "0");
                            int burst = int.Parse(row.Cells["BurstTime"].Value?.ToString() ?? "0");
                            int priority = 0;
                            if (selectedAlgorithm == "Priority Scheduling")
                                priority = int.Parse(row.Cells["Priority"].Value?.ToString() ?? "0");
                            processes.Add(new Models.Process(id, arrival, burst, priority));
                        }
                    }
                    catch
                    {
                        return;
                    }
                    var resultFCFS = SchedulingAlgorithms.FCFS(processes);
                    avgWaitingTimes.Add("   FCFS", resultFCFS.Average(p => p.WaitingTime));
                    processes.Clear();
                    selectedAlgorithm = "SJF";
                    try
                    {
                        foreach (DataGridViewRow row in processGrid.Rows)
                        {
                            if (row.IsNewRow) continue;

                            string id = row.Cells["ProcessID"].Value?.ToString();
                            int arrival = int.Parse(row.Cells["ArrivalTime"].Value?.ToString() ?? "0");
                            int burst = int.Parse(row.Cells["BurstTime"].Value?.ToString() ?? "0");
                            int priority = 0;
                            if (selectedAlgorithm == "Priority Scheduling")
                                priority = int.Parse(row.Cells["Priority"].Value?.ToString() ?? "0");
                            processes.Add(new Models.Process(id, arrival, burst, priority));
                        }
                    }
                    catch
                    {
                        return;
                    }
                    var resultSJF = SchedulingAlgorithms.SJF(processes);
                    avgWaitingTimes.Add("    SJF", resultSJF.Average(p => p.WaitingTime));
                    processes.Clear();
                    selectedAlgorithm = "SJF Preemptive";
                    try
                    {
                        foreach (DataGridViewRow row in processGrid.Rows)
                        {
                            if (row.IsNewRow) continue;

                            string id = row.Cells["ProcessID"].Value?.ToString();
                            int arrival = int.Parse(row.Cells["ArrivalTime"].Value?.ToString() ?? "0");
                            int burst = int.Parse(row.Cells["BurstTime"].Value?.ToString() ?? "0");
                            int priority = 0;
                            if (selectedAlgorithm == "Priority Scheduling")
                                priority = int.Parse(row.Cells["Priority"].Value?.ToString() ?? "0");
                            processes.Add(new Models.Process(id, arrival, burst, priority));
                        }
                    }
                    catch
                    {
                        return;
                    }
                    var resultSJFPreemptive = SchedulingAlgorithms.SJFPreemptive(processes);
                    var finalSummarySJFPreemptive = GetSummaryResult(processes);
                    float totalWaitingSJFPreemptive = 0;
                    foreach (var p in finalSummarySJFPreemptive)
                    {
                        totalWaitingSJFPreemptive += p.WaitingTime;
                    }
                    avgWaitingTimes.Add("    SJF\nPreemptive", totalWaitingSJFPreemptive / (float)finalSummarySJFPreemptive.Count);
                    processes.Clear();
                    selectedAlgorithm = "Priority";
                    try
                    {
                        foreach (DataGridViewRow row in processGrid.Rows)
                        {
                            if (row.IsNewRow) continue;

                            string id = row.Cells["ProcessID"].Value?.ToString();
                            int arrival = int.Parse(row.Cells["ArrivalTime"].Value?.ToString() ?? "0");
                            int burst = int.Parse(row.Cells["BurstTime"].Value?.ToString() ?? "0");
                            int priority = 0;
                            if (selectedAlgorithm == "Priority Scheduling")
                                priority = int.Parse(row.Cells["Priority"].Value?.ToString() ?? "0");
                            processes.Add(new Models.Process(id, arrival, burst, priority));
                        }
                    }
                    catch
                    {
                        return;
                    }
                    var resultPriority = SchedulingAlgorithms.Priority(processes);
                    avgWaitingTimes.Add("  Priority", resultPriority.Average(p => p.WaitingTime));
                    processes.Clear();
                    selectedAlgorithm = "Priority Preemptive";
                    try
                    {
                        foreach (DataGridViewRow row in processGrid.Rows)
                        {
                            if (row.IsNewRow) continue;

                            string id = row.Cells["ProcessID"].Value?.ToString();
                            int arrival = int.Parse(row.Cells["ArrivalTime"].Value?.ToString() ?? "0");
                            int burst = int.Parse(row.Cells["BurstTime"].Value?.ToString() ?? "0");
                            int priority = 0;
                            if (selectedAlgorithm == "Priority Scheduling")
                                priority = int.Parse(row.Cells["Priority"].Value?.ToString() ?? "0");
                            processes.Add(new Models.Process(id, arrival, burst, priority));
                        }
                    }
                    catch
                    {
                        return;
                    }
                    var resultPriorityPreemptive = SchedulingAlgorithms.PriorityPreemptive(processes);
                    var finalSummaryPriorityPreemptive = GetSummaryResult(processes);
                    float totalWaitingPriorityPreemptive = 0;
                    foreach (var p in finalSummaryPriorityPreemptive)
                    {
                        totalWaitingPriorityPreemptive += p.WaitingTime;
                    }
                    avgWaitingTimes.Add("  Priority\nPreemptive", totalWaitingPriorityPreemptive / (float)finalSummaryPriorityPreemptive.Count);
                    processes.Clear();
                    selectedAlgorithm = "Round Roubin";
                    try
                    {
                        foreach (DataGridViewRow row in processGrid.Rows)
                        {
                            if (row.IsNewRow) continue;

                            string id = row.Cells["ProcessID"].Value?.ToString();
                            int arrival = int.Parse(row.Cells["ArrivalTime"].Value?.ToString() ?? "0");
                            int burst = int.Parse(row.Cells["BurstTime"].Value?.ToString() ?? "0");
                            int priority = 0;
                            if (selectedAlgorithm == "Priority Scheduling")
                                priority = int.Parse(row.Cells["Priority"].Value?.ToString() ?? "0");
                            processes.Add(new Models.Process(id, arrival, burst, priority));
                        }
                    }
                    catch
                    {
                        return;
                    }
                    var resultRoundRobin = SchedulingAlgorithms.RoundRobin(processes, 2);
                    var finalSummaryRoundRobin = GetSummaryResult(processes);
                    float totalWaitingRoundRobin = 0;
                    foreach (var p in finalSummaryRoundRobin)
                    {
                        totalWaitingRoundRobin += p.WaitingTime;
                    }
                    avgWaitingTimes.Add("Round\nRoubin", totalWaitingRoundRobin / (float)finalSummaryRoundRobin.Count);
                    double maxVal = avgWaitingTimes.Values.Max();

                    int i = 0;
                    foreach (var kvp in avgWaitingTimes)
                    {
                        double value = kvp.Value;
                        int barHeight = (int)(value / maxVal * chartHeight);

                        int x = startX + i * (barWidth + spacing);
                        int y = baseY - barHeight;

                        // Draw bar
                        g.FillRectangle(barBrush, x, y, barWidth, barHeight);

                        // Draw value above
                        g.DrawString($" {value:0.00}", labelFont, textBrush, x, y - 20);

                        // Draw label below
                        g.DrawString(kvp.Key, labelFont, textBrush, x - 5, baseY + 5);

                        i++;
                    }
                }
                catch (Exception ex)
                {
                    return;
                }
            };


            this.Controls.Add(groupBoxInput);
            this.Controls.Add(groupBoxResult);
            this.Controls.Add(groupAnalysis);
            
            groupBoxInput.Controls.Add(preemptiveRadio);
            groupBoxInput.Controls.Add(nonPreemptiveRadio);
            groupBoxInput.Controls.Add(algorithmComboBox);
            groupBoxInput.Controls.Add(quantumLabel);
            groupBoxInput.Controls.Add(quantumInput);
            groupBoxInput.Controls.Add(calculateButton);
            groupBoxInput.Controls.Add(importButton);
            groupBoxInput.Controls.Add(exportButton);
            groupBoxInput.Controls.Add(processGrid);

            groupBoxResult.Controls.Add(resultGrid);
            groupBoxResult.Controls.Add(avgWaitingLabel);
            groupBoxResult.Controls.Add(avgTurnaroundLabel);
            groupBoxResult.Controls.Add(bestAlgorithmLabel);
            groupBoxResult.Controls.Add(avgBestAlgorithmLabel);

            groupAnalysis.Controls.Add(groupAnalysis1);
            groupAnalysis1.Controls.Add(simulationPanel);
            groupAnalysis.Controls.Add(groupAnalysis2);
            groupAnalysis2.Controls.Add(piePanel);
            groupAnalysis.Controls.Add(groupAnalysis3);
            groupAnalysis3.Controls.Add(histogramPanel);
            
            this.Controls.Add(drawPanel);
            drawPanel.SendToBack();

            algorithmComboBox.SelectedIndexChanged += (_, __) => RepositionLayout();
            algorithmComboBox.SelectedIndex = 0;

            processGrid.EnableHeadersVisualStyles = false;
            processGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Black;
            processGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;

            resultGrid.EnableHeadersVisualStyles = false;
            resultGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Black;
            resultGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        }

        private void RepositionLayout()
        {
            MaxSimulationProgress = (this.ClientSize.Width - 20) / 3 - 160;
            groupBoxInput.Width = this.ClientSize.Width / 2 - 20;
            groupBoxResult.Width = this.ClientSize.Width / 2 - 20;
            groupBoxResult.Location = new Point(10 + this.ClientSize.Width / 2, 10);
            groupAnalysis.Width = this.ClientSize.Width - 20;
            groupAnalysis.Location = new Point(10, groupBoxInput.Bottom + 10);
            groupAnalysis1.Width = groupAnalysis2.Width = groupAnalysis3.Width = (this.ClientSize.Width - 60) / 3;
            groupAnalysis1.Location = new Point(10, 30);
            groupAnalysis2.Location = new Point(20 + groupAnalysis2.Width, 30);
            groupAnalysis3.Location = new Point(30 + groupAnalysis2.Width + groupAnalysis3.Width, 30);
            simulationPanel.Width = (this.ClientSize.Width - 60) / 3;
            piePanel.Width = groupAnalysis2.Width;
            histogramPanel.Width = groupAnalysis2.Width;
            int margin = 10;
            int gridHeight = 150;

            algorithmComboBox.Left = margin;
            algorithmComboBox.Top = margin * 3;

            bool showQuantum = algorithmComboBox.SelectedItem?.ToString() == "Round Robin";
            quantumLabel.Visible = quantumInput.Visible = showQuantum;
            preemptiveRadio.Visible = nonPreemptiveRadio.Visible = !showQuantum;

            if(showQuantum)
            {
                algorithmComboBox.Width = groupBoxInput.Width - 2 * margin - (15 + quantumLabel.Width + quantumInput.Width);
            }
            else
            {
                algorithmComboBox.Width = groupBoxInput.Width - 2 * margin - (15 + preemptiveRadio.Width + nonPreemptiveRadio.Width);
            }
            bool FCFS = algorithmComboBox.SelectedItem?.ToString() == "First Come First Serve (FCFS)";
            if (FCFS)
            {
                algorithmComboBox.Width = groupBoxInput.Width - 2 * margin;
                quantumLabel.Visible = quantumInput.Visible = preemptiveRadio.Visible = nonPreemptiveRadio.Visible = !FCFS;
            }

            preemptiveRadio.Left = algorithmComboBox.Right + 10;
            preemptiveRadio.Top = algorithmComboBox.Top + 5;

            nonPreemptiveRadio.Left = preemptiveRadio.Right + 10;
            nonPreemptiveRadio.Top = preemptiveRadio.Top;

            quantumLabel.Left = algorithmComboBox.Right + 10;
            quantumLabel.Top = algorithmComboBox.Top + 5;

            quantumInput.Left = quantumLabel.Right + 5;
            quantumInput.Top = quantumLabel.Top - 2;

            bool showPriority = algorithmComboBox.SelectedItem?.ToString() == "Priority Scheduling";
            processGrid.Columns["Priority"].Visible = showPriority;

            processGrid.Left = margin;
            processGrid.Top = algorithmComboBox.Bottom + margin;
            processGrid.Width = this.ClientSize.Width - (5 * margin + 140 + this.ClientSize.Width / 2);
            processGrid.Height = gridHeight + avgTurnaroundLabel.Height + avgWaitingLabel.Height - algorithmComboBox.Height + 10;

            calculateButton.Left = processGrid.Right + margin;
            calculateButton.Top = processGrid.Top;
            calculateButton.Width = 140;

            importButton.Left = calculateButton.Left;
            importButton.Top = calculateButton.Bottom + 10;
            importButton.Width = 140;

            exportButton.Left = importButton.Left;
            exportButton.Top = importButton.Bottom + 10;
            exportButton.Width = 140;

            resultGrid.Width = groupBoxResult.Width - 2 * margin;
            resultGrid.Height = gridHeight;
            resultGrid.Left = margin;
            resultGrid.Top = 3 * margin;

            avgWaitingLabel.Left = margin;
            avgWaitingLabel.Top = resultGrid.Bottom + margin;

            avgTurnaroundLabel.Left = margin;
            avgTurnaroundLabel.Top = avgWaitingLabel.Bottom + 10;

            bestAlgorithmLabel.Left = avgWaitingLabel.Right + 10;
            bestAlgorithmLabel.Top = resultGrid.Bottom + margin;

            avgBestAlgorithmLabel.Left = avgWaitingLabel.Right + 10;
            avgBestAlgorithmLabel.Top = avgWaitingLabel.Bottom + 10;
        }

        private Brush GetBrushForProcess(Process p)
        {
            string name = p.Name;
            int hash = Math.Abs(name.GetHashCode());
            Color[] palette = new Color[] { Color.Brown, Color.DarkRed, Color.Green, Color.OrangeRed, Color.Blue, Color.OrangeRed, Color.DeepPink };
            p.color = palette[hash % palette.Length];
            return new SolidBrush(palette[hash % palette.Length]);
        }

        private void DrawPanel_Paint(object sender, PaintEventArgs e)
        {
            if (result == null || result.Count == 0) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int panelWidth = drawPanel.ClientSize.Width;
            int panelHeight = drawPanel.ClientSize.Height;

            int totalTime = result.Sum(p => p.BurstTime); ;
            if (totalTime == 0) return;
            if (CurrentTime != 0)
            {
                totalTime = CurrentTime;
            }

            int margin = 50;
            int barHeight = 25;
            int barTop = this.ClientSize.Height - barHeight - margin;

            int startX = margin;
            int availableWidth = panelWidth - 2 * margin;
            float unitWidth = (float)availableWidth / totalTime;
            foreach (var p in result)
            {
                int boxWidth = (int)(p.BurstTime * unitWidth);
                if (startX + boxWidth > this.ClientSize.Width)
                {
                    boxWidth = this.ClientSize.Width - startX - margin;
                }
                Rectangle box = new Rectangle(startX, barTop, boxWidth, barHeight);
                using (var brush = GetBrushForProcess(p))
                {
                    g.FillRectangle(brush, box);
                }
                g.DrawRectangle(Pens.Black, box);
                g.DrawString(p.Name, this.Font, Brushes.White, box.X + boxWidth / 2, box.Y + 5);
                startX += boxWidth;
                if (startX >= (float)availableWidth)
                {
                    foreach(var a in animatedProcesses)
                    {
                        if (p.Name == a.Name && !p.Drawn)
                        {
                            int tot = 0;
                            foreach(var s in result)
                            {
                                if(s.Name == a.Name)
                                {
                                    tot += s.BurstTime;
                                }
                            }
                            a.Width += (MaxSimulationProgress / tot);
                            if (a.Width >= MaxSimulationProgress)
                            {
                                p.Drawn = true;
                                a.Width = MaxSimulationProgress;
                            }
                            break;
                        }
                    }
                    break;
                }
                foreach (var a in animatedProcesses)
                {
                    if (p.Name == a.Name && !p.Drawn)
                    {
                        if (a.Width >= MaxSimulationProgress)
                        {
                            p.Drawn = true;
                            a.Width = MaxSimulationProgress;
                        }
                    }
                }
            }
            startX = margin;
            for (int i = 0; i <= totalTime; i++)
            {
                g.DrawString(i.ToString(), this.Font, Brushes.Black, startX, barTop + barHeight + 5);
                startX += (int)(unitWidth);
            }
        }

        void DrawPieChart(Graphics g, Rectangle area, List<Models.Process> processes)
        {
            if (processes == null || processes.Count == 0) return;

            float total = processes.Sum(p => p.BurstTime);
            float startAngle = 0;

            Random rand = new Random();
            Font labelFont = new Font("Segoe UI", 9);
            Brush textBrush = Brushes.Black;

            int legendY = 10;
            var finalSummary = GetSummaryResult(processes);
            int i = 0;
            int t = CurrentTime;
            if(CurrentTime == 0)
            {
                t = (int)total;
            }
            foreach (var p in finalSummary)
            {
                int tot = 0;
                int tot2 = 0;
                foreach (var a in result)
                {
                    for (int k = 0; k < a.BurstTime; k++)
                    {
                        tot2++;
                    }
                    tot2 -= (a.BurstTime - 1);
                    if(p.Name == a.Name)
                    {
                        for (int k = 0; k < a.BurstTime; k++)
                        {
                            if (i < t && tot2 <= t)
                            {
                                i++;
                                tot++;
                            }
                            if (tot2 >= t)
                                break;
                        }
                        if (tot2 >= t)
                            break;
                    }
                    tot2 += (a.BurstTime - 1);
                    if (i >= t)
                        break;
                }
                float sweepAngle = 0;
                if(t != 0)
                    sweepAngle = (360f / (float)t);
                else
                    sweepAngle = (360f / total);

                float endAngle = startAngle + sweepAngle * tot;
                if(sweepAngle != 0)
                {
                    for (float j = startAngle; j < endAngle && j < 360; j += sweepAngle)
                    {
                        using (Brush b = new SolidBrush(p.color))
                        {
                            g.FillPie(b, area, startAngle, sweepAngle);
                        }
                        startAngle += sweepAngle;
                    }
                }

                // Draw legend
                g.FillRectangle(new SolidBrush(p.color), area.Right + 10, legendY, 15, 15);
                g.DrawString($"{p.Name} ({(p.BurstTime / total * 100):0.#}%)", labelFont, textBrush, area.Right + 30, legendY);

                legendY += 20;
            }
        }

        private void CalculateButton_Click(object sender, EventArgs e)
        {
            string selectedAlgorithm = algorithmComboBox.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedAlgorithm)) return;

            List<Models.Process> processes = new List<Models.Process>();

            try
            {
                foreach (DataGridViewRow row in processGrid.Rows)
                {
                    if (row.IsNewRow) continue;

                    string id = row.Cells["ProcessID"].Value?.ToString();
                    int arrival = int.Parse(row.Cells["ArrivalTime"].Value?.ToString() ?? "0");
                    int burst = int.Parse(row.Cells["BurstTime"].Value?.ToString() ?? "0");
                    int priority = 0;
                    if (selectedAlgorithm == "Priority Scheduling")
                        priority = int.Parse(row.Cells["Priority"].Value?.ToString() ?? "0");

                    processes.Add(new Models.Process(id, arrival, burst, priority));
                }
            }
            catch
            {
                MessageBox.Show("Please make sure all process fields are filled correctly.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Detect duplicate names
            var duplicateNames = processes
                .GroupBy(p => p.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames.Count > 0)
            {
                MessageBox.Show(
                    "Duplicate process names found:\n" + string.Join(", ", duplicateNames),
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return; // ❌ أوقف تنفيذ الحساب
            }
            // Run selected algorithm
            switch (selectedAlgorithm)
            {
                case "First Come First Serve (FCFS)":
                    result = SchedulingAlgorithms.FCFS(processes);
                    break;
                case "Shortest Job First (SJF)":
                    result = nonPreemptiveRadio.Checked ? SchedulingAlgorithms.SJF(processes) : SchedulingAlgorithms.SJFPreemptive(processes);
                    break;
                case "Priority Scheduling":
                    result = nonPreemptiveRadio.Checked ? SchedulingAlgorithms.Priority(processes) : SchedulingAlgorithms.PriorityPreemptive(processes);
                    break;
                case "Round Robin":
                    int q = (int)quantumInput.Value;
                    result = SchedulingAlgorithms.RoundRobin(processes, q);
                    break;
            }

            var finalSummary = GetSummaryResult(processes);
            foreach(var p in processes)
            {
                foreach(var a in result)
                {
                    if(a.Name==p.Name)
                    {
                        a.WaitingTime += p.WaitingTime;
                    }
                }
            }
            

            if (result == null || result.Count == 0)
            {
                MessageBox.Show("No processes were scheduled.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Display result
            resultGrid.Rows.Clear();
            double totalWaiting = 0, totalTurnaround = 0;
            tt.Interval = 100;
            tt.Start();
            foreach (var p in finalSummary)
            {
                resultGrid.Rows.Add(p.Name, p.WaitingTime, p.TurnaroundTime, p.BurstTime, p.ArrivalTime);
                totalWaiting += p.WaitingTime;
                totalTurnaround += p.TurnaroundTime;
            }

            avgWaitingLabel.Text = $"Average Waiting Time: {(totalWaiting / finalSummary.Count):0.00}";
            avgTurnaroundLabel.Text = $"Average Turnaround Time: {(totalTurnaround / finalSummary.Count):0.00}";
            bestAlgorithmLabel.Text = $"Best Algorithm: {SchedulingAlgorithms.BestAlgorithm(finalSummary)}";
            avgBestAlgorithmLabel.Text = $"Average Waiting Best Algorithm: {SchedulingAlgorithms.BestAlgorithmAvgWaiting(finalSummary)}";

            simulationPanel.Controls.Clear();


            int y = 10;
            animatedProcesses.Clear();
            foreach (var p in finalSummary)
            {
                var brush = GetBrushForProcess(p);
                // Label
                Label lbl = new Label
                {
                    Text = p.Name + ":",
                    Font = new Font("Segoe UI", 10),
                    Location = new Point(35, y + 5),
                    AutoSize = true
                };

                // ProgressBar
                Panel bar = new Panel
                {
                    Name = p.Name,
                    Width = 0,
                    Height = 20,
                    Location = new Point(95, y + 6),
                    BackColor = p.color
                };

                animatedProcesses.Add(bar);
                simulationPanel.Controls.Add(lbl);
                simulationPanel.Controls.Add(bar);

                y += 35;
            }
            lastSummary = finalSummary;
            histogramPanel.Invalidate();
        }

        public static List<Process> GetSummaryResult(List<Process> processes)
        {
            return processes.Select(p =>
            {
                var newP = new Process(p.Name, p.ArrivalTime, p.BurstTime, p.Priority);
                newP.StartTime = p.StartTime;
                newP.FinishTime = p.FinishTime;
                newP.WaitingTime = p.WaitingTime;
                newP.TurnaroundTime = p.TurnaroundTime;
                newP.color = p.color;
                return newP;
            }).ToList();
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text files (*.txt)|*.txt";
            if (openFileDialog.ShowDialog() != DialogResult.OK) return;

            string filePath = openFileDialog.FileName;
            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
            {
                MessageBox.Show("File format invalid or empty.");
                return;
            }

            string selectedAlgorithm = algorithmComboBox.SelectedItem?.ToString();
            bool isPriority = selectedAlgorithm == "Priority Scheduling";
            bool isRoundRobin = selectedAlgorithm == "Round Robin";

            // Check headers
            string[] headers = lines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> requiredHeaders = new List<string> { "ProcessID", "ArrivalTime", "BurstTime" };
            if (isPriority) requiredHeaders.Add("Priority");

            if (!requiredHeaders.All(h => headers.Contains(h)))
            {
                MessageBox.Show($"Missing required columns for {selectedAlgorithm}.\nRequired: {string.Join(", ", requiredHeaders)}");
                return;
            }

            // Clear old data
            processGrid.Rows.Clear();

            // Parse data rows
            try
            {
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] parts = lines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < requiredHeaders.Count) continue;

                    int colIndex = 0;
                    string id = parts[colIndex++];
                    int arrival = int.Parse(parts[colIndex++]);
                    int burst = int.Parse(parts[colIndex++]);
                    string priority = isPriority ? parts[colIndex] : "";

                    if (isPriority)
                        processGrid.Rows.Add(id, arrival, burst, priority);
                    else
                        processGrid.Rows.Add(id, arrival, burst);
                }
            }
            catch
            {
                MessageBox.Show("Failed to import file. Make sure data is formatted correctly.");
                return;
            }

            // Show quantum input if Round Robin and get it from filename if possible
            if (isRoundRobin)
            {
                quantumLabel.Visible = quantumInput.Visible = true;

                // Try get quantum from filename like "RoundRobin_q3.txt"
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                if (fileName.Contains("q"))
                {
                    string[] parts = fileName.Split('q');
                    if (parts.Length > 1 && int.TryParse(parts[1], out int q))
                        quantumInput.Value = Math.Min(Math.Max(q, quantumInput.Minimum), quantumInput.Maximum);
                }
            }
            else
            {
                quantumLabel.Visible = quantumInput.Visible = false;
            }

            //MessageBox.Show("Data imported successfully ✅");
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            if (result == null || result.Count == 0)
            {
                MessageBox.Show("No results to export. Please calculate first.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output.txt");
                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.WriteLine("Process\tArrival\tBurst\tWaiting\tTurnaround");
                    foreach (var p in result)
                    {
                        writer.WriteLine($"{p.Name}\t{p.ArrivalTime}\t{p.BurstTime}\t{p.WaitingTime}\t{p.TurnaroundTime}");
                    }

                    double avgWait = result.Average(p => p.WaitingTime);
                    double avgTurn = result.Average(p => p.TurnaroundTime);

                    writer.WriteLine();
                    writer.WriteLine($"Average Waiting Time: {avgWait:0.00}");
                    writer.WriteLine($"Average Turnaround Time: {avgTurn:0.00}");
                }

                MessageBox.Show("Results exported successfully to output.txt!", "Export Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
