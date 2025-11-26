using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

// Use actual DTOs from main project
using AINarrator;

namespace AINarrator.Test
{
    /// <summary>
    /// Interactive test console for AI Narrator LLM integration.
    /// Tests narration and choice generation with mock colony scenarios.
    /// </summary>
    class Program
    {
        private static TestConfig _config;
        private static TestOpenRouterClient _client;

        static void Main(string[] args)
        {
            Console.Title = "AI Narrator - LLM Test Console";
            PrintBanner();

            // Load or create configuration
            if (!LoadConfiguration())
            {
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            _client = new TestOpenRouterClient(_config);

            // Test connection first
            if (!TestApiConnection())
            {
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            // Main menu loop
            RunMainMenu();
        }

        static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════════╗
║         TALES FROM THE RIMWORLD - LLM TEST CONSOLE            ║
║                                                               ║
║  Test AI-generated narration and choices with mock scenarios  ║
╚═══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        static bool LoadConfiguration()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            
            if (!File.Exists(configPath))
            {
                // Try current directory
                configPath = "appsettings.json";
            }

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    _config = JsonConvert.DeserializeObject<TestConfig>(json);
                    
                    if (_config.ApiKey == "YOUR_OPENROUTER_API_KEY_HERE" || string.IsNullOrEmpty(_config.ApiKey))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("API key not configured in appsettings.json");
                        Console.ResetColor();
                        return PromptForApiKey();
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Configuration loaded");
                    Console.WriteLine($"  Model: {_config.Model}");
                    Console.WriteLine($"  Temperature: {_config.Temperature}");
                    Console.ResetColor();
                    return true;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error loading configuration: {ex.Message}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("appsettings.json not found");
                Console.ResetColor();
            }

            return PromptForApiKey();
        }

        static bool PromptForApiKey()
        {
            Console.WriteLine();
            Console.WriteLine("Please enter your OpenRouter API key:");
            Console.Write("> ");
            string apiKey = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("API key is required.");
                Console.ResetColor();
                return false;
            }

            _config = _config ?? new TestConfig();
            _config.ApiKey = apiKey;

            Console.WriteLine();
            Console.WriteLine("Enter model name (press Enter for default: google/gemini-2.0-flash-001):");
            Console.Write("> ");
            string model = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(model))
            {
                _config.Model = model;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Using model: {_config.Model}");
            Console.ResetColor();
            return true;
        }

        static bool TestApiConnection()
        {
            Console.WriteLine();
            Console.WriteLine("Testing API connection...");

            string error;
            if (_client.TestConnection(out error))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ API connection successful!");
                Console.ResetColor();
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ API connection failed: {error}");
                Console.ResetColor();
                return false;
            }
        }

        static void RunMainMenu()
        {
            while (true)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.WriteLine("                        MAIN MENU                              ");
                Console.WriteLine("═══════════════════════════════════════════════════════════════");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("  [1] Test Event Narration");
                Console.WriteLine("  [2] Test Choice Event Generation");
                Console.WriteLine("  [3] Run Full Scenario Test (Narration + Choice)");
                Console.WriteLine("  [4] Test All Scenarios");
                Console.WriteLine("  [5] Change Model/Settings");
                Console.WriteLine("  [0] Exit");
                Console.WriteLine();
                Console.Write("Select option: ");

                string input = Console.ReadLine()?.Trim();

                switch (input)
                {
                    case "1":
                        TestEventNarration();
                        break;
                    case "2":
                        TestChoiceGeneration();
                        break;
                    case "3":
                        TestFullScenario();
                        break;
                    case "4":
                        TestAllScenarios();
                        break;
                    case "5":
                        ChangeSettings();
                        break;
                    case "0":
                    case "exit":
                    case "quit":
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Invalid option");
                        Console.ResetColor();
                        break;
                }
            }
        }

        static MockColonyContext SelectScenario()
        {
            Console.WriteLine();
            Console.WriteLine("Select Colony Scenario:");
            Console.WriteLine("  [1] Early Game - Fresh crash landing (Day 3)");
            Console.WriteLine("  [2] Mid Game - Established colony (Day 45)");
            Console.WriteLine("  [3] Late Game - Wealthy fortress (Day 180)");
            Console.WriteLine("  [4] Crisis - Colony in dire straits");
            Console.WriteLine();
            Console.Write("Select: ");

            string input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("→ Early Game Scenario selected");
                    Console.ResetColor();
                    return MockScenarios.GetEarlyGameScenario();
                case "2":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("→ Mid Game Scenario selected");
                    Console.ResetColor();
                    return MockScenarios.GetMidGameScenario();
                case "3":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("→ Late Game Scenario selected");
                    Console.ResetColor();
                    return MockScenarios.GetLateGameScenario();
                case "4":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("→ Crisis Scenario selected");
                    Console.ResetColor();
                    return MockScenarios.GetCrisisScenario();
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Invalid selection, using Mid Game");
                    Console.ResetColor();
                    return MockScenarios.GetMidGameScenario();
            }
        }

        static MockEvent SelectEvent()
        {
            Console.WriteLine();
            Console.WriteLine("Select Event Type:");
            Console.WriteLine("  [1] Small Raid");
            Console.WriteLine("  [2] Large Raid");
            Console.WriteLine("  [3] Trader Arrival");
            Console.WriteLine("  [4] Wanderer Joins");
            Console.WriteLine("  [5] Solar Flare");
            Console.WriteLine("  [6] Psychic Drone");
            Console.WriteLine("  [7] Manhunter Pack");
            Console.WriteLine("  [8] Cold Snap");
            Console.WriteLine("  [9] Mechanoid Cluster");
            Console.WriteLine();
            Console.Write("Select: ");

            string input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1": return MockEvent.RaidSmall;
                case "2": return MockEvent.RaidLarge;
                case "3": return MockEvent.TraderArrival;
                case "4": return MockEvent.WandererJoins;
                case "5": return MockEvent.SolarFlare;
                case "6": return MockEvent.PsychicDrone;
                case "7": return MockEvent.ManhunterPack;
                case "8": return MockEvent.ColdSnap;
                case "9": return MockEvent.MechanoidCluster;
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Invalid selection, using Small Raid");
                    Console.ResetColor();
                    return MockEvent.RaidSmall;
            }
        }

        static void TestEventNarration()
        {
            var scenario = SelectScenario();
            var mockEvent = SelectEvent();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"═══ Testing Event Narration: {mockEvent.Label} ═══");
            Console.ResetColor();
            Console.WriteLine();

            // Build prompts
            string systemPrompt = TestPromptBuilder.GetNarrationSystemPrompt();
            string userPrompt = TestPromptBuilder.BuildEventPrompt(scenario, mockEvent);

            // Show what we're sending
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("--- Sending to LLM ---");
            Console.WriteLine($"Context length: ~{userPrompt.Length} chars");
            Console.ResetColor();

            try
            {
                Console.WriteLine();
                Console.WriteLine("Requesting narration...");
                Console.WriteLine();

                string narration = _client.RequestNarration(systemPrompt, userPrompt);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                    THE NARRATOR SPEAKS                       ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine(WrapText(narration, 70));
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void TestChoiceGeneration()
        {
            var scenario = SelectScenario();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("═══ Testing Choice Event Generation ═══");
            Console.ResetColor();
            Console.WriteLine();

            string systemPrompt = TestPromptBuilder.GetChoiceSystemPrompt();
            string userPrompt = TestPromptBuilder.BuildChoicePrompt(scenario);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("--- Sending to LLM ---");
            Console.WriteLine($"Context length: ~{userPrompt.Length} chars");
            Console.ResetColor();

            try
            {
                Console.WriteLine();
                Console.WriteLine("Requesting choice event (this may take a moment)...");
                Console.WriteLine();

                var response = _client.RequestChoiceEvent(systemPrompt, userPrompt);

                if (response.Success && response.Events.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"✓ Received {response.Events.Count} choice event(s)");
                    Console.ResetColor();
                    Console.WriteLine();

                    int eventNum = 1;
                    foreach (var choiceEvent in response.Events)
                    {
                        PrintChoiceEvent(choiceEvent, eventNum++);
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ Failed to parse choice events: {response.Error}");
                    Console.ResetColor();
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("Raw response:");
                    Console.WriteLine(response.RawContent?.Substring(0, Math.Min(500, response.RawContent?.Length ?? 0)));
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void PrintChoiceEvent(ChoiceEvent evt, int eventNumber)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║                  CHOICE EVENT #{eventNumber}                            ║");
            Console.WriteLine($"╚══════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine(WrapText(evt.NarrativeText, 70));
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("OPTIONS:");
            Console.ResetColor();

            int optionNum = 1;
            foreach (var option in evt.Options)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"  [{optionNum}] {option.Label}");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"      Hint: {option.HintText}");
                if (option.Consequences != null && option.Consequences.Count > 0)
                {
                    int consequenceNum = 1;
                    foreach (var consequence in option.Consequences)
                    {
                        string paramsStr = consequence.Parameters != null && consequence.Parameters.Count > 0
                            ? JsonConvert.SerializeObject(consequence.Parameters)
                            : "{}";
                        Console.WriteLine($"      Consequence {consequenceNum++}: {consequence.Type} {paramsStr}");
                    }
                }
                Console.ResetColor();
                optionNum++;
            }
            Console.WriteLine();
        }

        static void TestFullScenario()
        {
            var scenario = SelectScenario();
            var mockEvent = SelectEvent();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"  FULL SCENARIO TEST: {scenario.ColonyName} (Day {scenario.ColonyAgeDays})");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.ResetColor();

            // Part 1: Event Narration
            Console.WriteLine();
            Console.WriteLine("PART 1: Event Narration");
            Console.WriteLine("------------------------");

            try
            {
                string narration = _client.RequestNarration(
                    TestPromptBuilder.GetNarrationSystemPrompt(),
                    TestPromptBuilder.BuildEventPrompt(scenario, mockEvent));

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine();
                Console.WriteLine(WrapText(narration, 70));
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Narration Error: {ex.Message}");
                Console.ResetColor();
            }

            // Part 2: Choice Event
            Console.WriteLine();
            Console.WriteLine("PART 2: Choice Event");
            Console.WriteLine("--------------------");

            try
            {
                var response = _client.RequestChoiceEvent(
                    TestPromptBuilder.GetChoiceSystemPrompt(),
                    TestPromptBuilder.BuildChoicePrompt(scenario));

                if (response.Success && response.Events.Count > 0)
                {
                    // Show just one event (randomly selected in real game)
                    var selectedEvent = response.Events[0];
                    PrintChoiceEvent(selectedEvent, 1);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Choice Error: {response.Error}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Choice Error: {ex.Message}");
                Console.ResetColor();
            }
        }

        static void TestAllScenarios()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("              TESTING ALL SCENARIOS                            ");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.ResetColor();

            var scenarios = new List<(string Name, MockColonyContext Context, MockEvent Event)>
            {
                ("Early Game - Raid", MockScenarios.GetEarlyGameScenario(), MockEvent.RaidSmall),
                ("Early Game - Wanderer", MockScenarios.GetEarlyGameScenario(), MockEvent.WandererJoins),
                ("Mid Game - Trade", MockScenarios.GetMidGameScenario(), MockEvent.TraderArrival),
                ("Mid Game - Raid", MockScenarios.GetMidGameScenario(), MockEvent.RaidLarge),
                ("Late Game - Mechanoids", MockScenarios.GetLateGameScenario(), MockEvent.MechanoidCluster),
                ("Crisis - Cold Snap", MockScenarios.GetCrisisScenario(), MockEvent.ColdSnap)
            };

            foreach (var (name, context, evt) in scenarios)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"─── {name} ───");
                Console.ResetColor();

                try
                {
                    string narration = _client.RequestNarration(
                        TestPromptBuilder.GetNarrationSystemPrompt(),
                        TestPromptBuilder.BuildEventPrompt(context, evt));

                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(WrapText(narration, 70));
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.ResetColor();
                }

                Console.WriteLine();
                Console.WriteLine("Press any key for next scenario...");
                Console.ReadKey(true);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n✓ All scenarios tested!");
            Console.ResetColor();
        }

        static void ChangeSettings()
        {
            Console.WriteLine();
            Console.WriteLine("Current Settings:");
            Console.WriteLine($"  Model: {_config.Model}");
            Console.WriteLine($"  Temperature: {_config.Temperature}");
            Console.WriteLine($"  Max Narration Tokens: {_config.MaxNarrationTokens}");
            Console.WriteLine($"  Max Choice Tokens: {_config.MaxChoiceTokens}");
            Console.WriteLine($"  Debug Mode: {_config.DebugMode}");
            Console.WriteLine();

            Console.WriteLine("Enter new model name (or press Enter to keep current):");
            Console.Write("> ");
            string model = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(model))
            {
                _config.Model = model;
            }

            Console.WriteLine("Enter new temperature 0.0-2.0 (or press Enter to keep current):");
            Console.Write("> ");
            string tempStr = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(tempStr) && float.TryParse(tempStr, out float temp))
            {
                _config.Temperature = Math.Max(0, Math.Min(2, temp));
            }

            Console.WriteLine("Toggle debug mode? (y/n, or press Enter to keep current):");
            Console.Write("> ");
            string debugStr = Console.ReadLine()?.Trim().ToLower();
            if (debugStr == "y" || debugStr == "yes")
            {
                _config.DebugMode = !_config.DebugMode;
            }

            // Recreate client with new settings
            _client = new TestOpenRouterClient(_config);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Settings updated - Model: {_config.Model}, Temp: {_config.Temperature}, Debug: {_config.DebugMode}");
            Console.ResetColor();
        }

        static string WrapText(string text, int width)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var lines = new List<string>();
            var words = text.Split(' ');
            var currentLine = "";

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 <= width)
                {
                    currentLine += (currentLine.Length > 0 ? " " : "") + word;
                }
                else
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine);
                    }
                    currentLine = word;
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine);
            }

            return string.Join("\n", lines);
        }
    }
}

