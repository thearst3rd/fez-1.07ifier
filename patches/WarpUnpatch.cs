using FezEngine.Services.Scripting;
using FezEngine.Structure.Input;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Components.Actions;
using FezGame.Structure;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Text;
using FezGame.Services;
using FezEngine.Components;
using System.Reflection;
using MonoMod;
using FezEngine.Services;

namespace Fez107ifier.patches
{
	internal class WarpUnpatch
	{
		private IDetour EnterDoorTestConditionsDetour;

		private MethodInfo UnDotizeMethod;
		private MethodInfo GetDestinationMethod;

		[ServiceDependency]
		public IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public IInputManager InputManager { private get; set; }

		[ServiceDependency]
		public IGameStateManager GameState { private get; set; }

		[ServiceDependency]
		public IDotManager DotManager { private get; set; }

		[ServiceDependency]
		public IGameLevelManager LevelManager { private get; set; }

		[ServiceDependency]
		public IContentManagerProvider CMProvider { private get; set; }

		[ServiceDependency]
		public IGameCameraManager CameraManager { private get; set; }

		[ServiceDependency]
		public IWalkToService WalkTo { private get; set; }

		public WarpUnpatch()
		{
			ServiceHelper.InjectServices(this);
			EnterDoorTestConditionsDetour = new Hook(
				typeof(EnterDoor).GetMethod("TestConditions", BindingFlags.NonPublic | BindingFlags.Instance),
				EnterDoorTestConditionsHooked);
			UnDotizeMethod = typeof(EnterDoor).GetMethod("UnDotize", BindingFlags.NonPublic | BindingFlags.Instance);
			GetDestinationMethod = typeof(EnterDoor).GetMethod("GetDestination", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		public void Dispose()
		{
			EnterDoorTestConditionsDetour.Dispose();
		}

		// Copied from FezGame.Components.Actions.EnterDoor.TestConditions, adapted to work as a hook
		// Man I had to do way too much just to add five entries to the switch statement at the top here
		private void EnterDoorTestConditionsHooked(Action<EnterDoor> original, EnterDoor self)
		{
			switch (PlayerManager.Action)
			{
				case ActionType.Idle:
				case ActionType.LookingLeft:
				case ActionType.LookingRight:
				case ActionType.LookingUp:
				case ActionType.LookingDown:
				case ActionType.Walking:
				// !! UNPATCH HERE
				// These five actiontypes were removed from 1.07 to 1.08
				case ActionType.Jumping:
				case ActionType.Lifting:
				case ActionType.Falling:
				case ActionType.Bouncing:
				case ActionType.Flying:
				// !! UNPATCH ENDS HERE
				case ActionType.Running:
				case ActionType.Dropping:
				case ActionType.Sliding:
				case ActionType.Landing:
				case ActionType.Teetering:
				case ActionType.IdlePlay:
				case ActionType.IdleSleep:
				case ActionType.IdleLookAround:
				case ActionType.IdleYawn:
					{
						string text = PlayerManager.NextLevel;
						if (PlayerManager.NextLevel == "CABIN_INTERIOR_A")
						{
							text = "CABIN_INTERIOR_B";
						}
						if (InputManager.RotateLeft == FezButtonState.Down || InputManager.RotateRight == FezButtonState.Down)
						{
							//UnDotize();
							UnDotizeMethod.Invoke(self, null);
							break;
						}
						if (PlayerManager.NextLevel == "SKULL_B" && ServiceHelper.Get<ITombstoneService>().get_AlignedCount() < 4)
						{
							//UnDotize();
							UnDotizeMethod.Invoke(self, null);
							break;
						}
						if (PlayerManager.NextLevel == "ZU_HEADS" && !GameState.SaveData.World.ContainsKey("ZU_HEADS"))
						{
							ISuckBlockService suckBlockService = ServiceHelper.Get<ISuckBlockService>();
							bool flag = false;
							for (int i = 2; i < 6; i++)
							{
								if (!suckBlockService.get_IsSucked(i))
								{
									flag = true;
								}
							}
							if (flag)
							{
								//UnDotize();
								UnDotizeMethod.Invoke(self, null);
								break;
							}
						}
						bool skipPreview = (bool)typeof(EnterDoor).GetField("skipPreview", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self);
						if (PlayerManager.DoorVolume.HasValue && PlayerManager.Grounded && !PlayerManager.HideFez && PlayerManager.CanControl && !PlayerManager.Background && !DotManager.PreventPoI && GameState.SaveData.World.ContainsKey(text) && !skipPreview && text != LevelManager.Name && LevelManager.Name != "CRYPT" && LevelManager.Name != "PYRAMID")
						{
							if (MemoryContentManager.AssetExists("Other Textures\\map_screens\\" + text.Replace('/', '\\')))
							{
								Texture2D destinationVignette = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/map_screens/" + text);
								DotManager.Behaviour = DotHost.BehaviourType.ThoughtBubble;
								DotManager.DestinationVignette = destinationVignette;
								if (text == "SEWER_QR" || text == "ZU_HOUSE_QR")
								{
									DotManager.DestinationVignetteSony = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/map_screens/" + text + "_SONY");
								}
								DotManager.ComeOut();
								if (DotManager.Owner != this)
								{
									DotManager.Hey();
								}
								DotManager.Owner = this;
							}
							else
							{
								//UnDotize();
								UnDotizeMethod.Invoke(self, null);
							}
						}
						else
						{
							//UnDotize();
							UnDotizeMethod.Invoke(self, null);
						}
						float step = (float)typeof(EnterDoor).GetField("step", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self);
						if (step != -1f || (InputManager.ExactUp != FezButtonState.Pressed && PlayerManager.LastAction != ActionType.OpeningDoor) || !PlayerManager.Grounded || !PlayerManager.DoorVolume.HasValue || PlayerManager.Background)
						{
							break;
						}
						//UnDotize();
						UnDotizeMethod.Invoke(self, null);
						FieldInfo skipFadeField = typeof(EnterDoor).GetField("skipFade", BindingFlags.NonPublic | BindingFlags.Instance);
						skipFadeField.SetValue(self, LevelManager.DestinationVolumeId.HasValue && PlayerManager.NextLevel == LevelManager.Name);
						GameState.SkipLoadScreen = (bool)skipFadeField.GetValue(self);
						bool spinThroughDoor = PlayerManager.SpinThroughDoor;
						if (spinThroughDoor)
						{
							Vector3 vector = CameraManager.Viewpoint.ForwardVector();
							Vector3 vector2 = CameraManager.Viewpoint.DepthMask();
							Volume volume = LevelManager.Volumes[PlayerManager.DoorVolume.Value];
							Vector3 vector3 = (volume.From + volume.To) / 2f;
							Vector3 vector4 = (volume.To - volume.From) / 2f;
							Vector3 vector5 = vector3 - vector4 * vector - vector;
							if (PlayerManager.Position.Dot(vector) < vector5.Dot(vector))
							{
								PlayerManager.Position = PlayerManager.Position * (Vector3.One - vector2) + vector5 * vector2;
							}
							//spinOrigin = GetDestination();
							//spinDestination = GetDestination() + vector * 1.5f;
							Vector3 destination = (Vector3)GetDestinationMethod.Invoke(self, null);
							typeof(EnterDoor).GetField("spinOrigin", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, destination);
							typeof(EnterDoor).GetField("spinDestination", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(self, destination + vector * 1.5f);
						}
						if (PlayerManager.CarriedInstance != null)
						{
							bool flag2 = PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsLight();
							PlayerManager.Position = (Vector3)GetDestinationMethod.Invoke(self, null);
							PlayerManager.Action = ((!flag2) ? (spinThroughDoor ? ActionType.EnterDoorSpinCarryHeavy : ActionType.CarryHeavyEnter) : (spinThroughDoor ? ActionType.EnterDoorSpinCarry : ActionType.CarryEnter));
						}
						else
						{
							WalkTo.Destination = () => (Vector3)GetDestinationMethod.Invoke(self, null);
							PlayerManager.Action = ActionType.WalkingTo;
							WalkTo.NextAction = (spinThroughDoor ? ActionType.EnterDoorSpin : ActionType.EnteringDoor);
						}
						break;
					}
				default:
					//UnDotize();
					UnDotizeMethod.Invoke(self, null);
					break;
			}
		}
	}
}
