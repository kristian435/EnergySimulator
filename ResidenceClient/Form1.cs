using Common.Models;
using System.Net.Http.Json;
using System.Windows.Forms.DataVisualization.Charting;

namespace ResidenceClient
{
    public partial class Form1 : Form
    {
        private readonly HttpClient _httpClient = new()
        {
            BaseAddress = new Uri("https://localhost:5002"),
            Timeout = TimeSpan.FromSeconds(8)
        };

        private System.Windows.Forms.Timer _refreshTimer = new();
        private List<Appliance> _appliances = new();
        private List<EnergyRecord> _history = new();
        private Chart? _mainChart;
        private Label? lblEnergyFlow;
        private Label? lblSimulatedTime;
        private long _currentSimulationSpeed = 60;
        private double ElectricityPricePerKwh = 0.25;
        private int PeopleCount = 4;
        private string HeatingMethod = "heat pump";
        private double Insulation = 0.75;
        private string CarType = "electric";
        private double MonthlyFee = 5.0;
        private bool _isReportMode = false;
        public Form1()
        {
            InitializeComponent();
            SetupAppliances();
            SetupDashboard();
            StartAutoRefresh();
        }

        private void SetupAppliances()
        {
            _appliances = new List<Appliance>
            {
                new Appliance { Name = "Sähköhella", MaxPowerKw = 2.2 },
                new Appliance { Name = "Pesukone", MaxPowerKw = 2.0 },
                new Appliance { Name = "Ilmalämpöpumppu", MaxPowerKw = 3.5 },
                new Appliance { Name = "Sähkösauna", MaxPowerKw = 6.0 },
                new Appliance { Name = "EV-laturi", MaxPowerKw = 11.0 }
            };
        }

        private void SetupDashboard()
        {
            this.Text = "Sähkönhaltijan Maailma - Simulaattori";
            this.Size = new Size(1720, 980);
            this.MinimumSize = new Size(1400, 800);
            this.BackColor = Color.FromArgb(25, 25, 35);

            // ====================== VASEN PANEELI ======================
            var leftPanel = new GroupBox
            {
                Text = "  KODINKONEET",
                Location = new Point(20, 20),
                Size = new Size(420, this.ClientSize.Height - 60),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(35, 35, 50),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };
            this.Controls.Add(leftPanel);

            int y = 40;
            foreach (var app in _appliances)
            {
                var chk = new CheckBox
                {
                    Text = $"{app.Name} ({app.MaxPowerKw} kW)",
                    Location = new Point(25, y),
                    ForeColor = Color.White,
                    Width = 360,
                    Height = 42,
                    Font = new Font("Segoe UI", 11.5f)
                };
                chk.CheckedChanged += (s, e) => { ToggleAppliance(app, chk.Checked); };
                leftPanel.Controls.Add(chk);
                y += 52;
            }

            // ====================== OIKEA PANEELI ======================
            var rightPanel = new Panel
            {
                Location = new Point(460, 20),
                Size = new Size(this.ClientSize.Width - 490, this.ClientSize.Height - 60),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(30, 30, 45)
            };
            this.Controls.Add(rightPanel);

            // Simulaation aika & nopeus
            CreateSection("SIMULAATION AIKA & NOPEUS", 20, 20, rightPanel);
            lblSimulatedTime = new Label
            {
                Location = new Point(20, 55),
                Size = new Size(520, 70),
                ForeColor = Color.Lime,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold)
            };
            rightPanel.Controls.Add(lblSimulatedTime);

            // Asetukset-nappi (siirretty parempaan paikkaan)
            var btnSettings = new Button
            {
                Text = "⚙️ Asetukset",
                Location = new Point(20, 140),
                Size = new Size(200, 55),
                BackColor = Color.FromArgb(70, 70, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnSettings.Click += (s, e) => OpenSettings();
            rightPanel.Controls.Add(btnSettings);

            // Nopeussäätimet
            var speeds = new[] { 1, 10, 60, 360, 3600 };
            int bx = 20;
            foreach (var speed in speeds)
            {
                var btn = new Button
                {
                    Text = $"{speed}x",
                    Location = new Point(bx, 210),
                    Size = new Size(80, 50),
                    BackColor = speed == 60 ? Color.Orange : Color.DodgerBlue,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold)
                };
                btn.Click += (s, e) => SetSimulationSpeed(speed);
                rightPanel.Controls.Add(btn);
                bx += 85;
            }

            // ====================== ENERGIANVIRTAUS ======================
            CreateSection("ENERGIANVIRTAUS - REAALIAIKA", 20, 280, rightPanel);

            lblEnergyFlow = new Label
            {
                Location = new Point(20, 315),
                Size = new Size(530, 260),
                ForeColor = Color.LightCyan,
                Font = new Font("Consolas", 11.8f),   // Console-tyylinen fontti
                BackColor = Color.FromArgb(20, 20, 32),
                Padding = new Padding(15),
                TextAlign = ContentAlignment.TopLeft
            };
            rightPanel.Controls.Add(lblEnergyFlow);

            // Raportit
            CreateSection("RAPORTIT", 20, 580, rightPanel);
            var btnHour = CreateButton("Tuntiraportti", 20, 620, rightPanel);
            var btnDay = CreateButton("Päiväraportti", 230, 620, rightPanel);
            var btnWeek = CreateButton("Viikkoraportti", 440, 620, rightPanel);

            btnHour.Click += (s, e) => ShowReport("hour");
            btnDay.Click += (s, e) => ShowReport("day");
            btnWeek.Click += (s, e) => ShowReport("week");

            // Pääkaavio
            _mainChart = new Chart
            {
                Location = new Point(20, 690),
                Size = new Size(rightPanel.Width - 40, rightPanel.Height - 720),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(35, 35, 50)
            };

            var area = new ChartArea { BackColor = Color.FromArgb(30, 30, 45) };
            _mainChart.ChartAreas.Add(area);

            var series = new Series
            {
                Name = "Kulutus",
                ChartType = SeriesChartType.Line,
                Color = Color.FromArgb(0, 170, 255),
                BorderWidth = 4,
                IsValueShownAsLabel = false,
                MarkerStyle = MarkerStyle.None
            };
            _mainChart.Series.Add(series);
            rightPanel.Controls.Add(_mainChart);
        }

        private Button CreateButton(string text, int x, int y, Control parent)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(180, 55),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            parent.Controls.Add(btn);
            return btn;
        }

        private void CreateSection(string title, int x, int y, Control parent)
        {
            parent.Controls.Add(new Label
            {
                Text = title,
                Location = new Point(x, y),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true
            });
        }

        private void ToggleAppliance(Appliance appliance, bool isOn)
        {
            appliance.IsOn = isOn;
            appliance.CurrentPowerKw = isOn ? Math.Round(appliance.MaxPowerKw * 0.85, 2) : 0;

            UpdateMainChart();                    // ← TÄRKEÄ
            _ = SendApplianceStatusToServer();
        }
        private void UpdateMainChart()
        {
            if (_mainChart == null || _isReportMode) return;

            var series = _mainChart.Series[0];
            series.Points.Clear();

            // Näytetään viimeiset 30 mittausta
            foreach (var rec in _history.TakeLast(30))
            {
                series.Points.AddXY(rec.Timestamp.ToString("HH:mm"), rec.ConsumptionKw);
            }

            _mainChart.Invalidate();
        }

        private async Task SendApplianceStatusToServer()
        {
            try
            {
                var status = new ApplianceStatusDto
                {
                    Appliances = new List<Appliance>(_appliances),
                    TotalConsumptionFromAppliancesKw = Math.Round(_appliances.Sum(a => a.CurrentPowerKw), 2)
                };
                await _httpClient.PostAsJsonAsync("/energy/appliances", status);
            }
            catch { }
        }

        private void StartAutoRefresh()
        {
            _refreshTimer.Stop();
            _refreshTimer.Tick -= RefreshTimer_Tick;

            // Mitä nopeampi simulaatio, sitä useammin haetaan dataa
            int interval = Math.Max(800, 3000 / (int)Math.Sqrt(_currentSimulationSpeed));

            _refreshTimer.Interval = interval;
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
        }
        private async void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            UpdateEnergyFlowDisplay();
            await UpdateSimulatedTime();
        }

        private async void UpdateEnergyFlowDisplay()
        {
            try
            {
                var flow = await _httpClient.GetFromJsonAsync<EnergyFlowDto>("/energy/flow");

                if (flow == null) return;

                // Päivitä historia
                _history.Add(new EnergyRecord
                {
                    Timestamp = flow.Timestamp,
                    ConsumptionKw = flow.ConsumptionKw
                });

                if (_history.Count > 500)
                    _history.RemoveAt(0);

                // Päivitä Energianvirtaus -teksti
                string energyText =
                    $"Kulutus:          {flow.ConsumptionKw:F2} kW\n" +
                    $"Aurinkotuotanto:   {flow.SolarProductionKw:F2} kW\n" +
                    $"Akku:              {flow.BatteryLevelKwh:F1} kWh ({flow.BatteryLevelPercent:F0}%)\n" +
                    $"Verkosta osto:     {flow.GridImportKw:F2} kW\n" +
                    $"Verkkoon myynti:   {flow.GridExportKw:F2} kW\n\n" +
                    $"Spot-hinta:        {flow.CurrentPricePerKwh:F3} €/kWh\n" +
                    $"Arvioitu tuntihinta: {(flow.ConsumptionKw * flow.CurrentPricePerKwh):F2} €/h";

                if (lblEnergyFlow != null)
                    lblEnergyFlow.Text = energyText;

                // Päivitä pääkaavio
                UpdateMainChart();   // ← Korjattu kutsutapa
            }
            catch (Exception ex)
            {
                if (lblEnergyFlow != null)
                    lblEnergyFlow.Text = $"Yhteys EnergyServeriin epäonnistui\n{ex.Message}";
            }
        }

        private async Task UpdateSimulatedTime()
        {
            if (lblSimulatedTime == null) return;
            try
            {
                var timeData = await _httpClient.GetFromJsonAsync<TimeResponseDto>("http://localhost:5000/time");
                if (timeData != null)
                {
                    lblSimulatedTime.Text = $"Simuloitu aika: {timeData.SimulatedTime:dd.MM.yyyy HH:mm:ss}\n" +
                                           $"Nopeus: {timeData.SimulationSpeed}x";

                    // Päivitetään paikallinen nopeusmuuttuja
                    if (_currentSimulationSpeed != timeData.SimulationSpeed)
                    {
                        _currentSimulationSpeed = timeData.SimulationSpeed;
                        StartAutoRefresh(); // Käynnistetään timer uudelleen uudella nopeudella
                    }
                }
            }
            catch { }
        }

        private async void SetSimulationSpeed(long speed)
        {
            try
            {
                _history.Clear();   // Tyhjennetään historia nopeuden vaihtuessa

                var config = new SimulationConfig { Speed = speed };
                await _httpClient.PostAsJsonAsync("http://localhost:5000/time/config", config);

                MessageBox.Show($"Simulaation nopeus muutettu → {speed}x\n\nHistoria tyhjennettiin.",
                               "Simulaatio", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Virhe nopeuden muuttamisessa:\n" + ex.Message, "Virhe",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task SendSettingsToEnergyServer()
        {
            try
            {
                var settings = new ResidenceSettings
                {
                    People = PeopleCount,
                    HeatingMethod = HeatingMethod,
                    Insulation = Insulation,
                    CarType = CarType,
                    MonthlyFee = MonthlyFee
                };

                var response = await _httpClient.PostAsJsonAsync("/settings", settings);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Asetukset lähetetty EnergyServerille onnistuneesti");
                }
                else
                {
                    Console.WriteLine($"❌ Virhe: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Virhe asetusten lähettämisessä: {ex.Message}");
            }
        }

        private void ShowReport(string period)
        {
            if (_mainChart == null || _history.Count < 5)
            {
                MessageBox.Show("Ei vielä tarpeeksi dataa.\nOdota hetki ja kokeile uudelleen.",
                                "Raportti", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _isReportMode = true;
            var series = _mainChart.Series[0];

            // Tallenna nykyinen tila
            var originalPoints = series.Points.Select(p => new DataPoint(p.XValue, p.YValues[0])).ToList();
            var originalType = series.ChartType;

            series.Points.Clear();

            DateTime simulatedNow = _history.LastOrDefault()?.Timestamp ?? DateTime.Now;
            List<EnergyRecord> dataToShow = new();
            string title = "";

            switch (period)
            {
                case "day":
                    dataToShow = _history.Where(r => r.Timestamp >= simulatedNow.AddHours(-24)).ToList();
                    title = "PÄIVÄRAPORTTI (24 simuloitua tuntia)";
                    break;

                case "week":
                    dataToShow = _history.Where(r => r.Timestamp >= simulatedNow.AddDays(-7)).ToList();
                    if (dataToShow.Count > 200)
                        dataToShow = dataToShow.Where((x, i) => i % 4 == 0).ToList();
                    title = "VIIKKORAPORTTI (7 simuloitua päivää)";
                    break;

                default: // hour
                    dataToShow = _history.Where(r => r.Timestamp >= simulatedNow.AddHours(-1)).ToList();
                    title = "TUNTIRAPORTTI (viimeinen simuloitu tunti)";
                    break;
            }

            series.ChartType = SeriesChartType.Line;

            double total = 0;
            foreach (var rec in dataToShow)
            {
                series.Points.AddXY(rec.Timestamp.ToString("HH:mm"), rec.ConsumptionKw);
                total += rec.ConsumptionKw;
            }

            double avgKw = dataToShow.Any() ? dataToShow.Average(r => r.ConsumptionKw) : 0;
            decimal estimatedCost = (decimal)(avgKw * Math.Max(dataToShow.Count / 12.0, 1)) * (decimal)ElectricityPricePerKwh;

            TimeSpan duration = dataToShow.Count > 1
                ? dataToShow.Last().Timestamp - dataToShow.First().Timestamp
                : TimeSpan.Zero;

            string durationText = duration.TotalHours >= 1
                ? $"{duration.TotalHours:F1} tuntia"
                : $"{duration.TotalMinutes:F0} minuuttia";

            // Täydellinen raportti
            MessageBox.Show($"{title}\n\n" +
                            $"Mittauksia: {dataToShow.Count}\n" +
                            $"Kokonaiskulutus: {total:F1} kWh\n" +
                            $"Keskimääräinen teho: {avgKw:F2} kW\n" +
                            $"Arvioitu hinta: {estimatedCost:F2} €\n\n" +
                            $"Simuloitu aikaväli: {durationText} ({_currentSimulationSpeed}x nopeudella)",
                            "Raportti", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Palauta alkuperäinen kaavio
            series.Points.Clear();
            foreach (var p in originalPoints)
            {
                series.Points.AddXY(p.XValue, p.YValues[0]);
            }
            series.ChartType = originalType;

            _mainChart.Invalidate();
            _isReportMode = false;
        }
        private void OpenSettings()
        {
            var settingsForm = new Form
            {
                Text = "Simulaation Asetukset",
                Size = new Size(620, 720),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(28, 28, 40),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Font = new Font("Segoe UI", 11f),
                MaximizeBox = false,
                MinimizeBox = false
            };

            int y = 30;
            int labelX = 40;
            int controlX = 300;

            // Asukasmäärä
            settingsForm.Controls.Add(new Label { Text = "Asukasmäärä taloudessa:", Location = new Point(labelX, y), ForeColor = Color.White, AutoSize = true });
            var numPeople = new NumericUpDown { Value = PeopleCount, Minimum = 1, Maximum = 10, Location = new Point(controlX, y), Width = 100 };
            settingsForm.Controls.Add(numPeople);
            y += 55;

            // Lämmitysmuoto
            settingsForm.Controls.Add(new Label { Text = "Lämmitysmuoto:", Location = new Point(labelX, y), ForeColor = Color.White, AutoSize = true });
            var cmbHeating = new ComboBox { Location = new Point(controlX, y), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbHeating.Items.AddRange(new[] { "wood", "oil", "heat pump", "direct electricity" });
            cmbHeating.Text = HeatingMethod;
            settingsForm.Controls.Add(cmbHeating);
            y += 55;

            // Eristystaso
            settingsForm.Controls.Add(new Label { Text = "Eristystaso (0.0 - 1.0):", Location = new Point(labelX, y), ForeColor = Color.White, AutoSize = true });
            var numInsulation = new NumericUpDown { Value = (decimal)Insulation, Minimum = 0, Maximum = 1, DecimalPlaces = 2, Increment = 0.05m, Location = new Point(controlX, y), Width = 100 };
            settingsForm.Controls.Add(numInsulation);
            y += 55;

            // Auton tyyppi
            settingsForm.Controls.Add(new Label { Text = "Auton tyyppi:", Location = new Point(labelX, y), ForeColor = Color.White, AutoSize = true });
            var cmbCar = new ComboBox { Location = new Point(controlX, y), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCar.Items.AddRange(new[] { "petrol", "hybrid", "electric" });
            cmbCar.Text = CarType;
            settingsForm.Controls.Add(cmbCar);
            y += 55;

            // Kuukausimaksu
            settingsForm.Controls.Add(new Label { Text = "Kuukausimaksu (€):", Location = new Point(labelX, y), ForeColor = Color.White, AutoSize = true });
            var numMonthly = new NumericUpDown { Value = (decimal)MonthlyFee, Minimum = 0, Maximum = 100, DecimalPlaces = 2, Increment = 0.5m, Location = new Point(controlX, y), Width = 100 };
            settingsForm.Controls.Add(numMonthly);
            y += 70;

            // Sähkön hinta (€/kWh)
            settingsForm.Controls.Add(new Label { Text = "Sähkön hinta (€/kWh):", Location = new Point(labelX, y), ForeColor = Color.White, AutoSize = true });
            var numPrice = new NumericUpDown
            {
                Value = (decimal)ElectricityPricePerKwh,
                Minimum = 0.01m,
                Maximum = 0.60m,
                DecimalPlaces = 3,
                Increment = 0.01m,
                Location = new Point(controlX, y),
                Width = 100
            };
            settingsForm.Controls.Add(numPrice);
            y += 55;

            // Tallenna nappi
            var btnSave = new Button
            {
                Text = "Tallenna asetukset",
                Location = new Point(200, y),
                Size = new Size(220, 65),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold)
            };

            btnSave.Click += (s, e) =>
            {
                PeopleCount = (int)numPeople.Value;
                HeatingMethod = cmbHeating.Text;
                Insulation = (double)numInsulation.Value;
                CarType = cmbCar.Text;
                MonthlyFee = (double)numMonthly.Value;
                ElectricityPricePerKwh = (double)numPrice.Value;

                // Lähetetään asetukset EnergyServerille
                _ = SendSettingsToEnergyServer();

                MessageBox.Show("Asetukset tallennettu ja lähetetty EnergyServerille!",
                                "Asetukset", MessageBoxButtons.OK, MessageBoxIcon.Information);
                settingsForm.Close();
            };

            settingsForm.Controls.Add(btnSave);
            settingsForm.ShowDialog();
        }
    }
}