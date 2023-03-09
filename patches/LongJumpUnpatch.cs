using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezEngine;
using FezGame.Components;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Microsoft.Xna.Framework.Audio;

namespace Fez107ifier.patches
{
	internal class LongJumpUnpatch
	{
		private IDetour PlayerActionsUpdateDetour;

		private FieldInfo OldLookDirField;
		private FieldInfo LastFrameField;
		private MethodInfo PlaySurfaceHitMethod;
		private FieldInfo IsLeftField;

		[ServiceDependency]
		public IGameCameraManager CameraManager { private get; set; }

		[ServiceDependency]
		public IGameStateManager GameState { private get; set; }

		[ServiceDependency]
		public IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public IInputManager InputManager { private get; set; }

		[ServiceDependency]
		public IPhysicsManager PhysicsManager { private get; set; }

		[ServiceDependency]
		public ICollisionManager CollisionManager { private get; set; }

		[ServiceDependency]
		public ILevelManager LevelManager { private get; set; }

		public LongJumpUnpatch()
		{
			ServiceHelper.InjectServices(this);
			PlayerActionsUpdateDetour = new Hook(
				typeof(PlayerActions).GetMethod("Update"),
				PlayerActionsUpdateHooked);
			OldLookDirField = typeof(PlayerActions).GetField("oldLookDir", BindingFlags.NonPublic | BindingFlags.Instance);
			LastFrameField = typeof(PlayerActions).GetField("lastFrame", BindingFlags.NonPublic | BindingFlags.Instance);
			PlaySurfaceHitMethod = typeof(PlayerActions).GetMethod("PlaySurfaceHit", BindingFlags.NonPublic | BindingFlags.Instance);
			IsLeftField = typeof(PlayerActions).GetField("isLeft", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		public void Dispose()
		{
			PlayerActionsUpdateDetour.Dispose();
		}

		// Copied from FezGame.Components.PlayerActions.Update, adapted to work as a hook
		private void PlayerActionsUpdateHooked(Action<PlayerActions, GameTime> original, PlayerActions self, GameTime gameTime)
		{
			if (GameState.Loading || PlayerManager.Hidden || GameState.InCutscene)
			{
				return;
			}
			PlayerManager.FreshlyRespawned = false;
			Vector3 position = PlayerManager.Position;
			if (!PlayerManager.CanControl)
			{
				InputManager.SaveState();
				InputManager.Reset();
			}
			if (CameraManager.Viewpoint != Viewpoint.Perspective && CameraManager.ActionRunning && !GameState.InMenuCube && !GameState.Paused && CameraManager.RequestedViewpoint == Viewpoint.None && !GameState.InMap && !LevelManager.IsInvalidatingScreen)
			{
				if (PlayerManager.Action.AllowsLookingDirectionChange() && !FezMath.AlmostEqual(InputManager.Movement.X, 0f))
				{
					//oldLookDir = PlayerManager.LookingDirection;
					OldLookDirField.SetValue(self, PlayerManager.LookingDirection);
					PlayerManager.LookingDirection = FezMath.DirectionFromMovement(InputManager.Movement.X);
				}
				Vector3 velocity = PlayerManager.Velocity;
				PhysicsManager.Update(PlayerManager);
				if (PlayerManager.Grounded && PlayerManager.Ground.NearLow == null)
				{
					TrileInstance farHigh = PlayerManager.Ground.FarHigh;
					Vector3 vector = CameraManager.Viewpoint.RightVector() * PlayerManager.LookingDirection.Sign();
					Vector3 vector2 = farHigh.Center - farHigh.TransformedSize / 2f * vector;
					Vector3 vector3 = PlayerManager.Center + PlayerManager.Size / 2f * vector;
					float num = (vector2 - vector3).Dot(vector);
					if (num > -0.25f)
					{
						PlayerManager.Position -= Vector3.UnitY * 0.01f * Math.Sign(CollisionManager.GravityFactor);
						if (farHigh.GetRotatedFace(CameraManager.Viewpoint.VisibleOrientation()) == CollisionType.AllSides)
						{
							PlayerManager.Position += num * vector;
							PlayerManager.Velocity = velocity * Vector3.UnitY;
						}
						else
						{
							PlayerManager.Velocity = velocity;
						}
						// !! UNPATCH HERE
						// This line was added in between 1.07 and 1.08, so we're removing it.
						//PlayerManager.GroundedVelocity = PlayerManager.Velocity;
						// !! UNPATCH ENDS HERE
						PlayerManager.Ground = default(MultipleHits<TrileInstance>);
					}
				}
				PlayerManager.RecordRespawnInformation();
				if (!PlayerManager.Action.HandlesZClamping() && ((HorizontalDirection)OldLookDirField.GetValue(self) != PlayerManager.LookingDirection || PlayerManager.LastAction == ActionType.RunTurnAround) && PlayerManager.Action != ActionType.Dropping && PlayerManager.Action != ActionType.GrabCornerLedge && PlayerManager.Action != ActionType.SuckedIn && PlayerManager.Action != ActionType.CrushVertical && PlayerManager.Action != ActionType.CrushHorizontal)
				{
					//CorrectWallOverlap(false);
					typeof(PlayerActions).GetMethod("CorrectWallOverlap", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(self, new object[] { false });
				}
			}
			if (PlayerManager.Grounded)
			{
				PlayerManager.IgnoreFreefall = false;
			}
			if (PlayerManager.Animation != null && (int)LastFrameField.GetValue(self) != PlayerManager.Animation.Timing.Frame)
			{
				if (PlayerManager.Grounded)
				{
					SurfaceType surfaceType = PlayerManager.Ground.First.Trile.SurfaceType;
					if (PlayerManager.Action == ActionType.Landing && PlayerManager.Animation.Timing.Frame == 0)
					{
						//PlaySurfaceHit(surfaceType, false);
						PlaySurfaceHitMethod.Invoke(self, new object[] { surfaceType, false });
					}
					else if ((PlayerManager.Action == ActionType.PullUpBack || PlayerManager.Action == ActionType.PullUpFront || PlayerManager.Action == ActionType.PullUpCornerLedge) && PlayerManager.Animation.Timing.Frame == 5)
					{
						//PlaySurfaceHit(surfaceType, false);
						PlaySurfaceHitMethod.Invoke(self, new object[] { surfaceType, false });
					}
					else if (PlayerManager.Action.GetAnimationPath() == "Walk")
					{
						if (PlayerManager.Animation.Timing.Frame == 1 || PlayerManager.Animation.Timing.Frame == 4)
						{
							if (PlayerManager.Action != ActionType.Sliding)
							{
								//(isLeft ? LeftStep : RightStep).EmitAt(PlayerManager.Position, RandomHelper.Between(-0.10000000149011612, 0.10000000149011612), RandomHelper.Between(0.89999997615814209, 1.0));
								bool isLeft = (bool)IsLeftField.GetValue(self);
								((SoundEffect)(typeof(PlayerActions).GetField(isLeft ? "LeftStep" : "RightStep", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self))).EmitAt(PlayerManager.Position, RandomHelper.Between(-0.10000000149011612, 0.10000000149011612), RandomHelper.Between(0.89999997615814209, 1.0));
								//isLeft = !isLeft;
								IsLeftField.SetValue(self, !isLeft);
							}
							//PlaySurfaceHit(surfaceType, false);
							PlaySurfaceHitMethod.Invoke(self, new object[] { surfaceType, false });
						}
					}
					else if (PlayerManager.Action == ActionType.Running)
					{
						if (PlayerManager.Animation.Timing.Frame == 0 || PlayerManager.Animation.Timing.Frame == 3)
						{
							//PlaySurfaceHit(surfaceType, true);
							PlaySurfaceHitMethod.Invoke(self, new object[] { surfaceType, true });
						}
					}
					else if (PlayerManager.CarriedInstance != null)
					{
						if (PlayerManager.Action.GetAnimationPath() == "CarryHeavyWalk")
						{
							if (PlayerManager.Animation.Timing.Frame == 0 || PlayerManager.Animation.Timing.Frame == 4)
							{
								//PlaySurfaceHit(surfaceType, true);
								PlaySurfaceHitMethod.Invoke(self, new object[] { surfaceType, true });
							}
						}
						else if (PlayerManager.Action.GetAnimationPath() == "CarryWalk" && (PlayerManager.Animation.Timing.Frame == 3 || PlayerManager.Animation.Timing.Frame == 7))
						{
							//PlaySurfaceHit(surfaceType, true);
							PlaySurfaceHitMethod.Invoke(self, new object[] { surfaceType, true });
						}
					}
					else
					{
						//isLeft = false;
						IsLeftField.SetValue(self, false);
					}
				}
				else
				{
					//isLeft = false;
					IsLeftField.SetValue(self, false);
				}
				//lastFrame = PlayerManager.Animation.Timing.Frame;
				LastFrameField.SetValue(self, PlayerManager.Animation.Timing.Frame);
			}
			if (!PlayerManager.CanControl)
			{
				InputManager.RecoverState();
			}
		}
	}
}
