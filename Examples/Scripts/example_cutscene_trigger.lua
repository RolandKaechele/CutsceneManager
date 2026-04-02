-- example_cutscene_trigger.lua
-- Demonstrates triggering a cutscene sequence from a Lua script
-- via a custom CutsceneManager event bridge.
--
-- This script is run by MapLoaderFramework.Runtime.LuaScriptLoader
-- through a CutsceneStepType.TriggerLua step.
--
-- Usage: Reference this file name in a CutsceneStep:
--   { "stepType": 11, "luaScript": "example_cutscene_trigger.lua" }

print("[CutsceneTrigger] Lua script executed at sequence step.")

-- Custom logic: set a flag, log, or call a registered C# callback.
-- (Expand by registering C# functions in LuaScriptLoader's script environment.)
