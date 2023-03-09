using Fez107ifier.patches;
using FEZUG.Features;
using FEZUG.Features.Console;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fez107ifier.fezug
{
	public class WarpUnpatchToggle : IFezugCommand
	{
		public string Name => "unpatchwarp";

		public string HelpText => "unpatchwarp [true/false] - Unpatches the warp bug from 1.07";

		public List<string> Autocomplete(string[] args)
		{
			return new string[] { "true", "false" }.Where(s => s.StartsWith(args[0])).ToList();
		}

		public bool Execute(string[] args)
		{
			if (args.Length != 1)
			{
				FezugConsole.Print($"Incorrect number of parameters: '{args.Length}'", FezugConsole.OutputType.Warning);
				return false;
			}

			if (args[0] != "true" && args[0] != "false")
			{
				FezugConsole.Print($"Invalid argument: '{args[0]}'", FezugConsole.OutputType.Warning);
				return false;
			}

			Fez107ifier.warpUnpatch.Enabled = args[0] == "true";
			FezugConsole.Print($"Warp bug is {(Fez107ifier.warpUnpatch.Enabled ? "un" : "")}patched.");

			return true;
		}
	}
}
