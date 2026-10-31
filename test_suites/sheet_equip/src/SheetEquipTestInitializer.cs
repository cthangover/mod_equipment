using System.Linq;
using Cthangover.Core.Mods;
using Cthangover.Core.Utils;
using Godot;

namespace Cthangover.Mods.Equipment.Tests
{
    /// <summary>
    /// Entry point of the sheet_equip integration test: queues the driver when the exact flag
    /// <c>--itest=sheet_equip</c> is given (the driver quits the process, so it must never run in
    /// another test). Optional <c>--shots=&lt;dir&gt;</c> makes the driver save screenshots.
    /// </summary>
    public class SheetEquipTestInitializer : IModInitializer
    {
        private static bool driverQueued;

        /// <summary>Nothing per mod.</summary>
        public void OnModLoaded(string modId)
        {
        }

        /// <summary>Queues the driver once.</summary>
        public void OnModResourcesReady()
        {
            var args = OS.GetCmdlineUserArgs();
            if (driverQueued || !args.Any(a => a == "--itest=sheet_equip"))
                return;

            driverQueued = true;
            var shots = args.Where(a => a.StartsWith("--shots=", System.StringComparison.Ordinal))
                .Select(a => a.Substring("--shots=".Length))
                .FirstOrDefault();
            GameLogger.Log("TEST", $"SheetEquipTestInitializer: activating sheet_equip, shots={shots ?? "off"}");
            ModRoot.AddDeferred(new SheetEquipTestDriver(shots));
        }
    }
}
