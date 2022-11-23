using System;
using System.Linq;
using UnityEngine;

namespace Unity.MegaCity.UI
{
    public interface IExecutionOptions
    {
        bool SkipMenu { get; }
        bool QuitAfterFlyover { get; }
    }

    /// <summary>
    /// save bool parameters such as SkipMenu or QuitAfterFlyover,
    /// These parameters are reading during the execution.
    /// These parameters can be writing using command line.
    /// </summary>
    public class ExecutionOptions : IExecutionOptions
    {
        public bool SkipMenu { get; }
        public bool QuitAfterFlyover { get; }

        public ExecutionOptions(bool skipMenu, bool quitAfterFlyover)
        {
            Console.WriteLine($"Creating execution options: skipMenu {skipMenu}, quitAfterFlyover {quitAfterFlyover}");

            SkipMenu = skipMenu;
            QuitAfterFlyover = quitAfterFlyover;
        }
    }

    /// <summary>
    /// This is a execution code to run via command line,
    /// This writes if the execution should skip the menu and close the execution after flyover the scene.
    /// </summary>
    public class ModeBootstrap : MonoBehaviour
    {
        const string runFlyoverAndExitSwitch = "--run-flyover-and-exit";

        public IExecutionOptions Options => options = options ?? CreateOptions();
        IExecutionOptions options;

        IExecutionOptions CreateOptions()
        {
            var automated = Environment.GetCommandLineArgs().Contains(runFlyoverAndExitSwitch);

            return new ExecutionOptions(automated, automated);
        }
    }
}
