namespace TCGPocketAutomation.TCGPocketAutomation
{
    public class ADBInstanceRealPhoneViaIP : ADBInstanceViaIP
    {
        protected override string LogHeader
        {
            get => $"{Name}\t{IP}:{Port}";
        }
    }
}
