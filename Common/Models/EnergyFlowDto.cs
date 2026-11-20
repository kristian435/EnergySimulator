namespace Common.Models;

public class EnergyFlowDto
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Energianvirtaus (kW)
    public double SolarProductionKw { get; set; }
    public double ConsumptionKw { get; set; }
    public double BatteryChargeKw { get; set; }      // positiivinen = lataus
    public double GridImportKw { get; set; }         // ostettu verkosta
    public double GridExportKw { get; set; }         // myyty verkkoon
    public double CurrentPricePerKwh { get; set; } = 0.0;

    // Akun tila
    public double BatteryLevelKwh { get; set; }
    public double BatteryLevelPercent { get; set; }

    public string StatusMessage { get; set; } = "Normaali toiminta";
}