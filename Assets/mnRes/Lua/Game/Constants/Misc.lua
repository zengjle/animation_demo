--[[
一些辅助性的定义
]]

-- 延迟时间定义
Delay = {
	wait10MilliSeconds = UnityEngine.WaitForSeconds(0.01),
	wait500MilliSeconds = UnityEngine.WaitForSeconds(0.5),
	wait1Second = UnityEngine.WaitForSeconds(1),
	wait3Seconds = UnityEngine.WaitForSeconds(3)
}

BehaviorTree = CS.BehaviorDesigner.Runtime.BehaviorTree
TaskStatus = CS.BehaviorDesigner.Runtime.Tasks.TaskStatus
