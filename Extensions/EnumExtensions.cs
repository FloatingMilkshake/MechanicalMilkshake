namespace MechanicalMilkshake.Extensions;

internal static class EnumExtensions
{
    extension(Enum e) {
        #nullable enable
        public string Humanize()
        {
            FieldInfo? field = e.GetType().GetField(e.ToString());
            DisplayAttribute? display = field?.GetCustomAttribute<DisplayAttribute>();

            return display?.GetName() ?? e.ToString();
        }
    }
}
