using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IDS.Core.IDS_CAN;
using IDS.Portable.Common;
using IDS.Portable.Common.Extensions;
using IDS.Portable.LogicalDevice;

namespace OneControl.Devices
{
	public class Leveler4RunnerSim : LogicalDeviceCommandRunnerIdsCanSim<LogicalDeviceLevelerStatusType4>, ITextConsole
	{
		private const string LogTag = "Leveler4RunnerSim";

		private Stack<LogicalDeviceLevelerScreenType4> _screenStack = new Stack<LogicalDeviceLevelerScreenType4>();

		private int _jackManualCount;

		private int _fillCount;

		private int _drainCount;

		private LogicalDeviceLevelerButtonAirSuspensionType4 _buttonAirSuspensionLastKnown;

		public LogicalDeviceLevelerScreenType4 CurrentScreen => LevelerStatus.ScreenSelected;

		public LogicalDeviceLevelerStatusType4 LevelerStatus { get; } = new LogicalDeviceLevelerStatusType4();


		public IDevice Device => null;

		public bool IsDetected => true;

		public IReadOnlyList<string> Lines => LinesRaw;

		public List<string> LinesRaw { get; set; } = new List<string> { "<Empty 1>", "<Empty 2>", "<Empty 3>", "<Empty 4>" };


		public TEXT_CONSOLE_SIZE Size { get; } = new TEXT_CONSOLE_SIZE(40, 4);


		public Leveler4RunnerSim()
			: base(new LogicalDeviceLevelerStatusType4(LogicalDeviceLevelerScreenType4.Home))
		{
			LevelerStatus.ScreenSelected = LogicalDeviceLevelerScreenType4.Home;
			SetText("No Test Message Set\nTest Line 2\nTest Line 3\nTest Line 4");
			GotoScreen(LogicalDeviceLevelerScreenType4.Home);
		}

		public static ILogicalDeviceLevelerCommandType4 MakeCommand(byte commandByte, byte[] data, uint dataSize, int responseTimeMs)
		{
			new LogicalDeviceLevelerCommandType4(commandByte, data, responseTimeMs);
			LogicalDeviceLevelerCommandType4.LevelerCommandCode levelerCommandCode = (LogicalDeviceLevelerCommandType4.LevelerCommandCode)commandByte;
			switch (levelerCommandCode)
			{
			case LogicalDeviceLevelerCommandType4.LevelerCommandCode.ButtonPress:
			{
				if (dataSize != 4 || data.Length < 4)
				{
					throw new ArgumentException($"{levelerCommandCode}: Unknown / Unexpected data length for command");
				}
				LogicalDeviceLevelerScreenType4 logicalDeviceLevelerScreenType = (LogicalDeviceLevelerScreenType4)data[0];
				int buttonsPressed = (data[1] << 16) | (data[2] << 8) | data[3];
				switch (logicalDeviceLevelerScreenType)
				{
				case LogicalDeviceLevelerScreenType4.Home:
					return new LogicalDeviceLevelerCommandButtonPressedHomeType4((LogicalDeviceLevelerButtonHomeType4)buttonsPressed, responseTimeMs);
				case LogicalDeviceLevelerScreenType4.AutoLevel:
				case LogicalDeviceLevelerScreenType4.AutoHitch:
				case LogicalDeviceLevelerScreenType4.AutoRetractAllJacks:
				case LogicalDeviceLevelerScreenType4.AutoRetractFrontJacks:
				case LogicalDeviceLevelerScreenType4.AutoRetractRearJacks:
				case LogicalDeviceLevelerScreenType4.AutoHomeJacks:
					return new LogicalDeviceLevelerCommandButtonPressedAutoOperationType4(logicalDeviceLevelerScreenType.ToOperationAuto(), responseTimeMs);
				case LogicalDeviceLevelerScreenType4.JackMovementManual:
					return new LogicalDeviceLevelerCommandButtonPressedJackMovementManualType4((LogicalDeviceLevelerButtonJackMovementManualType4)buttonsPressed, withConsole: false, responseTimeMs);
				case LogicalDeviceLevelerScreenType4.JackMovementManualConsole:
					return new LogicalDeviceLevelerCommandButtonPressedJackMovementManualType4((LogicalDeviceLevelerButtonJackMovementManualType4)buttonsPressed, withConsole: true, responseTimeMs);
				case LogicalDeviceLevelerScreenType4.JackMovementZero:
					return new LogicalDeviceLevelerCommandButtonPressedJackMovementZeroType4((LogicalDeviceLevelerButtonJackMovementZeroType4)buttonsPressed, responseTimeMs);
				case LogicalDeviceLevelerScreenType4.PromptInfo:
					return new LogicalDeviceLevelerCommandButtonPressedPromptInfoType4((LogicalDeviceLevelerButtonOkType4)buttonsPressed, responseTimeMs);
				case LogicalDeviceLevelerScreenType4.PromptYesNo:
					return new LogicalDeviceLevelerCommandButtonPressedPromptYesNoType4((LogicalDeviceLevelerButtonYesNoType4)buttonsPressed, responseTimeMs);
				case LogicalDeviceLevelerScreenType4.PromptAirbagTimeSelect:
					return new LogicalDeviceLevelerCommandButtonPressedPromptAirbagTimeSelectType4((LogicalDeviceLevelerButtonAirbagTimeSelectType4)buttonsPressed, responseTimeMs);
				case LogicalDeviceLevelerScreenType4.PromptFault:
					return new LogicalDeviceLevelerCommandButtonPressedFaultType4((LogicalDeviceLevelerButtonOkType4)buttonsPressed, responseTimeMs);
				case LogicalDeviceLevelerScreenType4.JackMovementFaultManual:
					return new LogicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType4((LogicalDeviceLevelerButtonJackMovementFaultManualType4)buttonsPressed, withConsole: false, responseTimeMs);
				case LogicalDeviceLevelerScreenType4.JackMovementFaultManualConsole:
					return new LogicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType4((LogicalDeviceLevelerButtonJackMovementFaultManualType4)buttonsPressed, withConsole: true, responseTimeMs);
				case LogicalDeviceLevelerScreenType4.AirSuspensionControlManual:
					return new LogicalDeviceLevelerCommandButtonAirSuspensionControlManualType4((LogicalDeviceLevelerButtonAirSuspensionType4)buttonsPressed, responseTimeMs);
				default:
					throw new ArgumentException($"{levelerCommandCode}: Unknown / Unexpected screen {logicalDeviceLevelerScreenType}");
				}
			}
			case LogicalDeviceLevelerCommandType4.LevelerCommandCode.Abort:
				if (dataSize != 0)
				{
					throw new ArgumentException($"{levelerCommandCode}: Unknown / Unexpected data length for command");
				}
				return new LogicalDeviceLevelerCommandAbortType4(responseTimeMs);
			case LogicalDeviceLevelerCommandType4.LevelerCommandCode.Back:
				if (dataSize != 1 || data.Length < 1)
				{
					throw new ArgumentException($"{levelerCommandCode}: Unknown / Unexpected data length for command");
				}
				return new LogicalDeviceLevelerCommandBackType4((LogicalDeviceLevelerScreenType4)data[0], responseTimeMs);
			case LogicalDeviceLevelerCommandType4.LevelerCommandCode.Home:
				if (dataSize != 0)
				{
					throw new ArgumentException($"{levelerCommandCode}: Unknown / Unexpected data length for command");
				}
				return new LogicalDeviceLevelerCommandHomeType4(responseTimeMs);
			default:
				TaggedLog.Debug("Leveler4RunnerSim", $"{levelerCommandCode}: Unknown / Unexpected command");
				return null;
			}
		}

		public override Task<CommandResult> SendCommandAsync(byte commandByte, byte[] data, uint dataSize, int responseTimeMs, CancellationToken cancelToken, Func<ILogicalDevice, CommandControl> cmdControl = null, CommandSendOption options = CommandSendOption.None)
		{
			try
			{
				ILogicalDeviceLevelerCommandType4 logicalDeviceLevelerCommandType = MakeCommand(commandByte, data, dataSize, responseTimeMs);
				if (!(logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandHomeType4))
				{
					if (!(logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandAbortType4))
					{
						if (!(logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandBackType4 logicalDeviceLevelerCommandBackType))
						{
							if (!(logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandButtonPressedHomeType4 logicalDeviceLevelerCommandButtonPressedHomeType))
							{
								if (!(logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandButtonPressedAutoOperationType4 logicalDeviceLevelerCommandButtonPressedAutoOperationType))
								{
									if (!(logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandButtonPressedJackMovementManualType4 logicalDeviceLevelerCommandButtonPressedJackMovementManualType))
									{
										if (!(logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandButtonPressedJackMovementZeroType4 logicalDeviceLevelerCommandButtonPressedJackMovementZeroType))
										{
											if (!(logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandButtonPressedPromptInfoType4 logicalDeviceLevelerCommandButtonPressedPromptInfoType))
											{
												if (!(logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandButtonPressedPromptYesNoType4 logicalDeviceLevelerCommandButtonPressedPromptYesNoType))
												{
													if (!(logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandButtonPressedPromptAirbagTimeSelectType4 logicalDeviceLevelerCommandButtonPressedPromptAirbagTimeSelectType))
													{
														if (!(logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandButtonPressedFaultType4 logicalDeviceLevelerCommandButtonPressedFaultType))
														{
															if (!(logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType4 logicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType))
															{
																if (logicalDeviceLevelerCommandType is LogicalDeviceLevelerCommandButtonAirSuspensionControlManualType4 logicalDeviceLevelerCommandButtonAirSuspensionControlManualType)
																{
																	if (logicalDeviceLevelerCommandButtonAirSuspensionControlManualType.ScreenSelected != CurrentScreen)
																	{
																		TaggedLog.Information("Leveler4RunnerSim", $"Button press {logicalDeviceLevelerCommandButtonAirSuspensionControlManualType.ButtonsPressed.DebugDumpAsFlags()} ignored as {logicalDeviceLevelerCommandButtonAirSuspensionControlManualType.ScreenSelected} doesn't match {CurrentScreen}");
																	}
																	else
																	{
																		if (logicalDeviceLevelerCommandButtonAirSuspensionControlManualType.ButtonsPressed.HasFlag(LogicalDeviceLevelerButtonAirSuspensionType4.Fill))
																		{
																			if (!_buttonAirSuspensionLastKnown.HasFlag(LogicalDeviceLevelerButtonAirSuspensionType4.Fill))
																			{
																				_fillCount++;
																			}
																			LevelerStatus.ButtonsEnabledRaw &= 4294967293u;
																			SetText($"Air Suspension Filling\nCount = {_fillCount}\nEvery 5 will cause fault\n19 will cause controller <Home>");
																			if (_fillCount % 5 == 0)
																			{
																				GotoScreen(LogicalDeviceLevelerScreenType4.PromptFault);
																			}
																			else if (_fillCount == 19)
																			{
																				GotoScreen(LogicalDeviceLevelerScreenType4.Home);
																			}
																		}
																		else if (logicalDeviceLevelerCommandButtonAirSuspensionControlManualType.ButtonsPressed.HasFlag(LogicalDeviceLevelerButtonAirSuspensionType4.Drain))
																		{
																			if (!_buttonAirSuspensionLastKnown.HasFlag(LogicalDeviceLevelerButtonAirSuspensionType4.Drain))
																			{
																				_drainCount++;
																			}
																			LevelerStatus.ButtonsEnabledRaw &= 4294967294u;
																			SetText($"Air Suspension Draining\nCount = {_drainCount} ({_drainCount % 5})\nEvery 5 will cause yes/no\n> 10 (on 5's) Prompt Info");
																			if (_drainCount % 5 == 0)
																			{
																				if (_drainCount > 11)
																				{
																					GotoScreen(LogicalDeviceLevelerScreenType4.PromptInfo);
																				}
																				else
																				{
																					GotoScreen(LogicalDeviceLevelerScreenType4.PromptYesNo);
																				}
																			}
																		}
																		else
																		{
																			LevelerStatus.ButtonsEnabledRaw |= 3u;
																			SetText("Air Suspension Ready");
																		}
																		_buttonAirSuspensionLastKnown = logicalDeviceLevelerCommandButtonAirSuspensionControlManualType.ButtonsPressed;
																	}
																}
																else
																{
																	TaggedLog.Debug("Leveler4RunnerSim", $"Simulation ignoring command {logicalDeviceLevelerCommandType}");
																}
															}
															else if (logicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType.ScreenSelected != CurrentScreen)
															{
																TaggedLog.Information("Leveler4RunnerSim", $"Button ignored as {logicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType.ScreenSelected} doesn't match {CurrentScreen}");
															}
															else if (logicalDeviceLevelerCommandButtonPressedJackMovementFaultManualType.ButtonsPressed.HasFlag(LogicalDeviceLevelerButtonJackMovementFaultManualType4.AutoRetract))
															{
																GotoAutoOperation(LogicalDeviceLevelerOperationAutoType4.AutoRetractAllJacks);
															}
														}
														else if (logicalDeviceLevelerCommandButtonPressedFaultType.ScreenSelected != CurrentScreen)
														{
															TaggedLog.Information("Leveler4RunnerSim", $"Button Press ignored as {logicalDeviceLevelerCommandButtonPressedFaultType.ScreenSelected} doesn't match {CurrentScreen}");
														}
														else if (logicalDeviceLevelerCommandButtonPressedFaultType.ButtonsPressed.HasFlag(LogicalDeviceLevelerButtonOkType4.Ok))
														{
															Back();
														}
													}
													else if (logicalDeviceLevelerCommandButtonPressedPromptAirbagTimeSelectType.ScreenSelected != CurrentScreen)
													{
														TaggedLog.Information("Leveler4RunnerSim", $"Button Press ignored as {logicalDeviceLevelerCommandButtonPressedPromptAirbagTimeSelectType.ScreenSelected} doesn't match {CurrentScreen}");
													}
													else if (logicalDeviceLevelerCommandButtonPressedPromptAirbagTimeSelectType.ButtonsPressed.HasFlag(LogicalDeviceLevelerButtonAirbagTimeSelectType4.Short) || logicalDeviceLevelerCommandButtonPressedPromptAirbagTimeSelectType.ButtonsPressed.HasFlag(LogicalDeviceLevelerButtonAirbagTimeSelectType4.Long))
													{
														Back();
													}
												}
												else if (logicalDeviceLevelerCommandButtonPressedPromptYesNoType.ScreenSelected != CurrentScreen)
												{
													TaggedLog.Information("Leveler4RunnerSim", $"Button Press ignored as {logicalDeviceLevelerCommandButtonPressedPromptYesNoType.ScreenSelected} doesn't match {CurrentScreen}");
												}
												else if (logicalDeviceLevelerCommandButtonPressedPromptYesNoType.ButtonsPressed.HasFlag(LogicalDeviceLevelerButtonYesNoType4.Yes) || logicalDeviceLevelerCommandButtonPressedPromptYesNoType.ButtonsPressed.HasFlag(LogicalDeviceLevelerButtonYesNoType4.No))
												{
													Back();
												}
											}
											else if (logicalDeviceLevelerCommandButtonPressedPromptInfoType.ScreenSelected != CurrentScreen)
											{
												TaggedLog.Information("Leveler4RunnerSim", $"Button Press ignored as {logicalDeviceLevelerCommandButtonPressedPromptInfoType.ScreenSelected} doesn't match {CurrentScreen}");
											}
											else if (logicalDeviceLevelerCommandButtonPressedPromptInfoType.ButtonsPressed.HasFlag(LogicalDeviceLevelerButtonOkType4.Ok))
											{
												Back();
											}
										}
										else if (logicalDeviceLevelerCommandButtonPressedJackMovementZeroType.ScreenSelected != CurrentScreen)
										{
											TaggedLog.Information("Leveler4RunnerSim", $"Button ignored as {logicalDeviceLevelerCommandButtonPressedJackMovementZeroType.ScreenSelected} doesn't match {CurrentScreen}");
										}
									}
									else if (logicalDeviceLevelerCommandButtonPressedJackMovementManualType.ScreenSelected != CurrentScreen)
									{
										TaggedLog.Information("Leveler4RunnerSim", $"Button Press ignored as {logicalDeviceLevelerCommandButtonPressedJackMovementManualType.ScreenSelected} doesn't match {CurrentScreen}");
									}
								}
								else if (logicalDeviceLevelerCommandButtonPressedAutoOperationType.ScreenSelected != CurrentScreen)
								{
									TaggedLog.Information("Leveler4RunnerSim", $"Button ignored as {logicalDeviceLevelerCommandButtonPressedAutoOperationType.ScreenSelected} doesn't match {CurrentScreen}");
								}
							}
							else if (logicalDeviceLevelerCommandButtonPressedHomeType.ScreenSelected != CurrentScreen)
							{
								TaggedLog.Information("Leveler4RunnerSim", $"Button ignored as {logicalDeviceLevelerCommandButtonPressedHomeType.ScreenSelected} doesn't match {CurrentScreen}");
							}
							else
							{
								switch (logicalDeviceLevelerCommandButtonPressedHomeType.ButtonsPressed)
								{
								case LogicalDeviceLevelerButtonHomeType4.AutoLevel:
									GotoScreen(LogicalDeviceLevelerScreenType4.AutoLevel);
									SetText("Auto Level");
									break;
								case LogicalDeviceLevelerButtonHomeType4.AutoHitch:
									GotoScreen(LogicalDeviceLevelerScreenType4.AutoHitch);
									SetText("Auto Hitch");
									break;
								case LogicalDeviceLevelerButtonHomeType4.AutoRetractAllJacks:
									GotoScreen(LogicalDeviceLevelerScreenType4.AutoRetractAllJacks);
									SetText("Auto Retract\nAll Jacks");
									break;
								case LogicalDeviceLevelerButtonHomeType4.AutoRetractFrontJacks:
									GotoScreen(LogicalDeviceLevelerScreenType4.AutoRetractFrontJacks);
									SetText("Auto Retract\nFront Jacks");
									break;
								case LogicalDeviceLevelerButtonHomeType4.AutoRetractRearJacks:
									GotoScreen(LogicalDeviceLevelerScreenType4.AutoRetractRearJacks);
									SetText("Auto Retract\nRear Jacks");
									break;
								case LogicalDeviceLevelerButtonHomeType4.ManualMode:
									GotoJackMovementManual(console: false);
									break;
								case LogicalDeviceLevelerButtonHomeType4.ManualAirSuspension:
									GotoJackManualAirSuspension();
									break;
								case LogicalDeviceLevelerButtonHomeType4.ZeroMode:
									GotoJackMovementZero();
									break;
								case LogicalDeviceLevelerButtonHomeType4.AutoHomeJacks:
									GoToAutoHomeJacks(displayPrompt: true);
									break;
								case LogicalDeviceLevelerButtonHomeType4.RfConfig:
									GotoJackPromptYesNo("RfConfig\nWould you like to pair?");
									break;
								}
							}
						}
						else if (logicalDeviceLevelerCommandBackType.ScreenSelected != CurrentScreen)
						{
							TaggedLog.Information("Leveler4RunnerSim", $"Button ignored as {logicalDeviceLevelerCommandBackType.ScreenSelected} doesn't match {CurrentScreen}");
						}
						else
						{
							Back();
						}
					}
					else
					{
						GotoScreen(LogicalDeviceLevelerScreenType4.Home);
					}
				}
				else
				{
					GotoScreen(LogicalDeviceLevelerScreenType4.Home);
				}
			}
			catch (Exception ex)
			{
				TaggedLog.Warning("Leveler4RunnerSim", $"{CurrentScreen}: SendCommandAsync failed {ex.Message}");
				throw;
			}
			return Task.FromResult(CommandResult.Completed);
		}

		private void SetText(string text)
		{
			lock (Lines)
			{
				string[] array = text?.Split(new char[1] { '\n' }) ?? new string[1] { "<Empty 1>" };
				for (int i = 0; i < Size.Height; i++)
				{
					string text2 = ((i >= array.Length) ? $"<Empty {i + 1}>" : array[i]).Truncate(Size.Width);
					if (i >= LinesRaw.Count)
					{
						LinesRaw.Add(text2);
					}
					else
					{
						LinesRaw[i] = text2;
					}
				}
			}
		}

		public void GotoScreen(LogicalDeviceLevelerScreenType4 screen)
		{
			switch (screen)
			{
			case LogicalDeviceLevelerScreenType4.Home:
				GotoHome();
				break;
			case LogicalDeviceLevelerScreenType4.AutoLevel:
			case LogicalDeviceLevelerScreenType4.AutoHitch:
			case LogicalDeviceLevelerScreenType4.AutoRetractAllJacks:
			case LogicalDeviceLevelerScreenType4.AutoRetractFrontJacks:
			case LogicalDeviceLevelerScreenType4.AutoRetractRearJacks:
				GotoAutoOperation(screen.ToOperationAuto());
				break;
			case LogicalDeviceLevelerScreenType4.AutoHomeJacks:
				GoToAutoHomeJacks(displayPrompt: false);
				break;
			case LogicalDeviceLevelerScreenType4.JackMovementManual:
				GotoJackMovementManual(console: false);
				break;
			case LogicalDeviceLevelerScreenType4.JackMovementManualConsole:
				GotoJackMovementManual(console: true);
				break;
			case LogicalDeviceLevelerScreenType4.JackMovementZero:
				GotoJackMovementZero();
				break;
			case LogicalDeviceLevelerScreenType4.PromptInfo:
				GotoJackPromptInfo();
				break;
			case LogicalDeviceLevelerScreenType4.PromptYesNo:
				GotoJackPromptYesNo();
				break;
			case LogicalDeviceLevelerScreenType4.PromptFault:
				GotoJackPromptFault();
				break;
			case LogicalDeviceLevelerScreenType4.JackMovementFaultManual:
				GotoJackMovementFaultManual(console: false);
				break;
			case LogicalDeviceLevelerScreenType4.JackMovementFaultManualConsole:
				GotoJackMovementFaultManual(console: true);
				break;
			case LogicalDeviceLevelerScreenType4.AirSuspensionControlManual:
				GotoJackManualAirSuspension();
				break;
			case LogicalDeviceLevelerScreenType4.Unknown:
				SetText($"Unknown Screen {screen}");
				break;
			}
		}

		public void Back()
		{
			if (_screenStack.Count == 0)
			{
				GotoScreen(LogicalDeviceLevelerScreenType4.Home);
				return;
			}
			_screenStack.Pop();
			GotoScreen((_screenStack.Count != 0) ? _screenStack.Peek() : LogicalDeviceLevelerScreenType4.Home);
		}

		public void GotoHome()
		{
			_screenStack.Clear();
			LevelerStatus.ScreenSelected = LogicalDeviceLevelerScreenType4.Home;
			LogicalDeviceLevelerButtonHomeType4 logicalDeviceLevelerButtonHomeType = LogicalDeviceLevelerButtonHomeType4.None;
			logicalDeviceLevelerButtonHomeType |= LogicalDeviceLevelerButtonHomeType4.AutoLevel;
			logicalDeviceLevelerButtonHomeType |= LogicalDeviceLevelerButtonHomeType4.AutoHitch;
			logicalDeviceLevelerButtonHomeType |= LogicalDeviceLevelerButtonHomeType4.AutoRetractAllJacks;
			logicalDeviceLevelerButtonHomeType |= LogicalDeviceLevelerButtonHomeType4.AutoRetractRearJacks;
			logicalDeviceLevelerButtonHomeType |= LogicalDeviceLevelerButtonHomeType4.ManualMode;
			logicalDeviceLevelerButtonHomeType |= LogicalDeviceLevelerButtonHomeType4.ManualAirSuspension;
			logicalDeviceLevelerButtonHomeType |= LogicalDeviceLevelerButtonHomeType4.ZeroMode;
			logicalDeviceLevelerButtonHomeType |= LogicalDeviceLevelerButtonHomeType4.AutoHomeJacks;
			logicalDeviceLevelerButtonHomeType |= LogicalDeviceLevelerButtonHomeType4.RfConfig;
			LevelerStatus.ButtonsEnabledRaw = (uint)logicalDeviceLevelerButtonHomeType;
			LevelerStatus.IsLevel = true;
			LevelerStatus.AreJacksFullyRetracted = false;
			LevelerStatus.AreJacksGrounded = true;
			LevelerStatus.AreJacksMoving = false;
			LevelerStatus.IsExcessAngleDetected = false;
			LevelerStatus.IsExcessTwistDetected = false;
			SetText("Home");
		}

		public void GotoAutoOperation(LogicalDeviceLevelerOperationAutoType4 operationAuto)
		{
			LevelerStatus.ScreenSelected = operationAuto.ToScreen();
			_screenStack.Push(LevelerStatus.ScreenSelected);
			LogicalDeviceLevelerButtonNoneType4 buttonsEnabledRaw = LogicalDeviceLevelerButtonNoneType4.None;
			LevelerStatus.ButtonsEnabledRaw = (uint)buttonsEnabledRaw;
			LevelerStatus.IsLevel = false;
			LevelerStatus.AreJacksFullyRetracted = false;
			LevelerStatus.AreJacksGrounded = false;
			LevelerStatus.AreJacksMoving = true;
			LevelerStatus.IsExcessAngleDetected = false;
			LevelerStatus.IsExcessTwistDetected = false;
			SetText($"Auto Operation\n{operationAuto}");
		}

		public void GoToAutoHomeJacks(bool displayPrompt)
		{
			if (displayPrompt)
			{
				LevelerStatus.ScreenSelected = LogicalDeviceLevelerScreenType4.AutoHomeJacks;
				_screenStack.Push(LevelerStatus.ScreenSelected);
				GotoJackPromptYesNo("Auto Home Jacks\nWould you like to begin?\n \n ");
			}
			else
			{
				GotoAutoOperation(LogicalDeviceLevelerOperationAutoType4.AutoHomeJacks);
			}
		}

		public void GotoJackMovementManual(bool console)
		{
			if (!console)
			{
				_jackManualCount++;
				if (_jackManualCount % 4 == 0)
				{
					GotoJackMovementFaultManual(console: true);
					return;
				}
				if (_jackManualCount % 6 == 0)
				{
					GotoJackMovementManual(console: true);
					return;
				}
			}
			LevelerStatus.ScreenSelected = (console ? LogicalDeviceLevelerScreenType4.JackMovementManualConsole : LogicalDeviceLevelerScreenType4.JackMovementManual);
			_screenStack.Push(LevelerStatus.ScreenSelected);
			LogicalDeviceLevelerButtonJackMovementManualType4 logicalDeviceLevelerButtonJackMovementManualType = LogicalDeviceLevelerButtonJackMovementManualType4.None;
			logicalDeviceLevelerButtonJackMovementManualType |= LogicalDeviceLevelerButtonJackMovementManualType4.JackRightFrontExtend;
			logicalDeviceLevelerButtonJackMovementManualType |= LogicalDeviceLevelerButtonJackMovementManualType4.JackRightFrontRetract;
			logicalDeviceLevelerButtonJackMovementManualType |= LogicalDeviceLevelerButtonJackMovementManualType4.JackLeftFrontExtend;
			logicalDeviceLevelerButtonJackMovementManualType |= LogicalDeviceLevelerButtonJackMovementManualType4.JackLeftFrontRetract;
			logicalDeviceLevelerButtonJackMovementManualType |= LogicalDeviceLevelerButtonJackMovementManualType4.JackRightRearExtend;
			logicalDeviceLevelerButtonJackMovementManualType |= LogicalDeviceLevelerButtonJackMovementManualType4.JackRightRearRetract;
			logicalDeviceLevelerButtonJackMovementManualType |= LogicalDeviceLevelerButtonJackMovementManualType4.JackLeftRearExtend;
			logicalDeviceLevelerButtonJackMovementManualType |= LogicalDeviceLevelerButtonJackMovementManualType4.JackLeftRearRetract;
			logicalDeviceLevelerButtonJackMovementManualType |= LogicalDeviceLevelerButtonJackMovementManualType4.JackTongueExtend;
			logicalDeviceLevelerButtonJackMovementManualType |= LogicalDeviceLevelerButtonJackMovementManualType4.JackTongueRetract;
			LevelerStatus.ButtonsEnabledRaw = (uint)logicalDeviceLevelerButtonJackMovementManualType;
			LevelerStatus.IsLevel = false;
			LevelerStatus.AreJacksFullyRetracted = false;
			LevelerStatus.AreJacksGrounded = false;
			LevelerStatus.AreJacksMoving = false;
			LevelerStatus.IsExcessAngleDetected = false;
			LevelerStatus.IsExcessTwistDetected = false;
			if (console)
			{
				SetText("Movement Manual\nWith Console");
			}
			else
			{
				SetText("Movement Manual\nNo Console");
			}
		}

		public void GotoJackMovementZero()
		{
			LevelerStatus.ScreenSelected = LogicalDeviceLevelerScreenType4.JackMovementZero;
			_screenStack.Push(LevelerStatus.ScreenSelected);
			LogicalDeviceLevelerButtonJackMovementZeroType4 logicalDeviceLevelerButtonJackMovementZeroType = LogicalDeviceLevelerButtonJackMovementZeroType4.None;
			logicalDeviceLevelerButtonJackMovementZeroType |= LogicalDeviceLevelerButtonJackMovementZeroType4.JackRightFrontExtend;
			logicalDeviceLevelerButtonJackMovementZeroType |= LogicalDeviceLevelerButtonJackMovementZeroType4.JackRightFrontRetract;
			logicalDeviceLevelerButtonJackMovementZeroType |= LogicalDeviceLevelerButtonJackMovementZeroType4.JackLeftFrontExtend;
			logicalDeviceLevelerButtonJackMovementZeroType |= LogicalDeviceLevelerButtonJackMovementZeroType4.JackLeftFrontRetract;
			logicalDeviceLevelerButtonJackMovementZeroType |= LogicalDeviceLevelerButtonJackMovementZeroType4.JackRightRearExtend;
			logicalDeviceLevelerButtonJackMovementZeroType |= LogicalDeviceLevelerButtonJackMovementZeroType4.JackRightRearRetract;
			logicalDeviceLevelerButtonJackMovementZeroType |= LogicalDeviceLevelerButtonJackMovementZeroType4.JackLeftRearExtend;
			logicalDeviceLevelerButtonJackMovementZeroType |= LogicalDeviceLevelerButtonJackMovementZeroType4.JackLeftRearRetract;
			logicalDeviceLevelerButtonJackMovementZeroType |= LogicalDeviceLevelerButtonJackMovementZeroType4.JackTongueExtend;
			logicalDeviceLevelerButtonJackMovementZeroType |= LogicalDeviceLevelerButtonJackMovementZeroType4.JackTongueRetract;
			logicalDeviceLevelerButtonJackMovementZeroType |= LogicalDeviceLevelerButtonJackMovementZeroType4.SetZeroPoint;
			LevelerStatus.ButtonsEnabledRaw = (uint)logicalDeviceLevelerButtonJackMovementZeroType;
			LevelerStatus.IsLevel = false;
			LevelerStatus.AreJacksFullyRetracted = false;
			LevelerStatus.AreJacksGrounded = false;
			LevelerStatus.AreJacksMoving = false;
			LevelerStatus.IsExcessAngleDetected = false;
			LevelerStatus.IsExcessTwistDetected = false;
			SetText("Movement Zero");
		}

		public void GotoJackPromptInfo()
		{
			LevelerStatus.ScreenSelected = LogicalDeviceLevelerScreenType4.PromptInfo;
			_screenStack.Push(LevelerStatus.ScreenSelected);
			LogicalDeviceLevelerButtonOkType4 logicalDeviceLevelerButtonOkType = LogicalDeviceLevelerButtonOkType4.None;
			logicalDeviceLevelerButtonOkType |= LogicalDeviceLevelerButtonOkType4.Ok;
			LevelerStatus.ButtonsEnabledRaw = (uint)logicalDeviceLevelerButtonOkType;
			LevelerStatus.IsLevel = false;
			LevelerStatus.AreJacksFullyRetracted = false;
			LevelerStatus.AreJacksGrounded = false;
			LevelerStatus.AreJacksMoving = false;
			LevelerStatus.IsExcessAngleDetected = false;
			LevelerStatus.IsExcessTwistDetected = false;
			SetText("Please press OK\nThis was a standard prompt test!");
		}

		public void GotoJackPromptYesNo(string prompt = null)
		{
			LevelerStatus.ScreenSelected = LogicalDeviceLevelerScreenType4.PromptYesNo;
			_screenStack.Push(LevelerStatus.ScreenSelected);
			LogicalDeviceLevelerButtonYesNoType4 logicalDeviceLevelerButtonYesNoType = LogicalDeviceLevelerButtonYesNoType4.None;
			logicalDeviceLevelerButtonYesNoType |= LogicalDeviceLevelerButtonYesNoType4.Yes;
			logicalDeviceLevelerButtonYesNoType |= LogicalDeviceLevelerButtonYesNoType4.No;
			LevelerStatus.ButtonsEnabledRaw = (uint)logicalDeviceLevelerButtonYesNoType;
			LevelerStatus.IsLevel = false;
			LevelerStatus.AreJacksFullyRetracted = false;
			LevelerStatus.AreJacksGrounded = false;
			LevelerStatus.AreJacksMoving = false;
			LevelerStatus.IsExcessAngleDetected = false;
			LevelerStatus.IsExcessTwistDetected = false;
			SetText(prompt ?? "Choose Yes or No\nThis was a standard yes/no prompt test!");
		}

		public void GotoAirbagTimePrompt(string prompt = null)
		{
			LevelerStatus.ScreenSelected = LogicalDeviceLevelerScreenType4.PromptAirbagTimeSelect;
			_screenStack.Push(LevelerStatus.ScreenSelected);
			LogicalDeviceLevelerButtonAirbagTimeSelectType4 logicalDeviceLevelerButtonAirbagTimeSelectType = LogicalDeviceLevelerButtonAirbagTimeSelectType4.None;
			logicalDeviceLevelerButtonAirbagTimeSelectType |= LogicalDeviceLevelerButtonAirbagTimeSelectType4.Short;
			logicalDeviceLevelerButtonAirbagTimeSelectType |= LogicalDeviceLevelerButtonAirbagTimeSelectType4.Long;
			LevelerStatus.ButtonsEnabledRaw = (uint)logicalDeviceLevelerButtonAirbagTimeSelectType;
			LevelerStatus.IsLevel = false;
			LevelerStatus.AreJacksFullyRetracted = false;
			LevelerStatus.AreJacksGrounded = false;
			LevelerStatus.AreJacksMoving = false;
			LevelerStatus.IsExcessAngleDetected = false;
			LevelerStatus.IsExcessTwistDetected = false;
			SetText(prompt ?? "Choose short or long\nThis was a standard short/long prompt!");
		}

		public void GotoJackPromptFault()
		{
			LevelerStatus.ScreenSelected = LogicalDeviceLevelerScreenType4.PromptFault;
			_screenStack.Push(LevelerStatus.ScreenSelected);
			LogicalDeviceLevelerButtonOkType4 logicalDeviceLevelerButtonOkType = LogicalDeviceLevelerButtonOkType4.None;
			logicalDeviceLevelerButtonOkType |= LogicalDeviceLevelerButtonOkType4.Ok;
			LevelerStatus.ButtonsEnabledRaw = (uint)logicalDeviceLevelerButtonOkType;
			LevelerStatus.IsLevel = false;
			LevelerStatus.AreJacksFullyRetracted = false;
			LevelerStatus.AreJacksGrounded = false;
			LevelerStatus.AreJacksMoving = false;
			LevelerStatus.IsExcessAngleDetected = false;
			LevelerStatus.IsExcessTwistDetected = false;
			SetText("Please press OK\nThis was a WARNING/FAULT\nprompt test!");
		}

		public void GotoJackMovementFaultManual(bool console)
		{
			LevelerStatus.ScreenSelected = (console ? LogicalDeviceLevelerScreenType4.JackMovementFaultManualConsole : LogicalDeviceLevelerScreenType4.JackMovementFaultManual);
			_screenStack.Push(LevelerStatus.ScreenSelected);
			LogicalDeviceLevelerButtonJackMovementFaultManualType4 logicalDeviceLevelerButtonJackMovementFaultManualType = LogicalDeviceLevelerButtonJackMovementFaultManualType4.None;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackRightFrontExtend;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackRightFrontRetract;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackLeftFrontExtend;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackLeftFrontRetract;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackRightRearExtend;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackRightRearRetract;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackLeftRearExtend;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackLeftRearRetract;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackTongueExtend;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackTongueRetract;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackRightMiddleExtend;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackRightMiddleRetract;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackLeftMiddleExtend;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.JackLeftMiddleRetract;
			logicalDeviceLevelerButtonJackMovementFaultManualType |= LogicalDeviceLevelerButtonJackMovementFaultManualType4.AutoRetract;
			LevelerStatus.ButtonsEnabledRaw = (uint)logicalDeviceLevelerButtonJackMovementFaultManualType;
			LevelerStatus.IsLevel = false;
			LevelerStatus.AreJacksFullyRetracted = false;
			LevelerStatus.AreJacksGrounded = false;
			LevelerStatus.AreJacksMoving = false;
			LevelerStatus.IsExcessAngleDetected = false;
			LevelerStatus.IsExcessTwistDetected = false;
			SetText("Movement Fault Manual\nWith Console");
		}

		public void GotoJackManualAirSuspension()
		{
			LevelerStatus.ScreenSelected = LogicalDeviceLevelerScreenType4.AirSuspensionControlManual;
			_screenStack.Push(LevelerStatus.ScreenSelected);
			LogicalDeviceLevelerButtonAirSuspensionType4 logicalDeviceLevelerButtonAirSuspensionType = LogicalDeviceLevelerButtonAirSuspensionType4.None;
			logicalDeviceLevelerButtonAirSuspensionType |= LogicalDeviceLevelerButtonAirSuspensionType4.Fill;
			logicalDeviceLevelerButtonAirSuspensionType |= LogicalDeviceLevelerButtonAirSuspensionType4.Drain;
			LevelerStatus.ButtonsEnabledRaw = (uint)logicalDeviceLevelerButtonAirSuspensionType;
			LevelerStatus.IsLevel = false;
			LevelerStatus.AreJacksFullyRetracted = false;
			LevelerStatus.AreJacksGrounded = false;
			LevelerStatus.AreJacksMoving = false;
			LevelerStatus.IsExcessAngleDetected = false;
			LevelerStatus.IsExcessTwistDetected = false;
			SetText("Air Suspension\nReady\n");
		}
	}
}
