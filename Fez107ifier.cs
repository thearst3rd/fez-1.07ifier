using Fez107ifier.patches;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using System;

namespace Fez107ifier
{
	public class Fez107ifier : GameComponent
	{
		private WarpUnpatch warpUnpatch;
		private LongJumpUnpatch longJumpUnpatch;

		public Fez107ifier(Game game) : base(game)
		{
		}

		public override void Initialize()
		{
			base.Initialize();
			warpUnpatch = new WarpUnpatch();
			longJumpUnpatch = new LongJumpUnpatch();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			warpUnpatch.Dispose();
			longJumpUnpatch.Dispose();
		}
	}
}
