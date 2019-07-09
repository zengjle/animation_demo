--[[
共享模块
]]

require 'Game.Components.Shared.Tasks.Tasks'
require 'Game.Components.Shared.Sequences.Sequences'
require 'Game.Components.Shared.Env.Env'
require 'Game.Components.Shared.Helper.Helper'

local Shared = Component('__shared__', Config.ViewConstants.top)

-- 用于存储task相关的交互数据，使用前，需要在这里提前声明定义
Shared.tasks = {}

-- 用来保存载入场景时的AsyncOperation
Shared.tasks.loadSceneAO = nil

-- 下一个要进入的context
Shared.tasks.nextContext = nil

function Shared:Initialize()
	print('[Shared] initialize')
	Env.Initialize()
end

function Shared:OnEnable()
	EventManager:AddListener(EventTypes.System_BeforeContextSwitched, Shared.OnBeforeContextSwitched)
	EventManager:AddListener(EventTypes.System_ContextSwitched, Shared.OnContextSwitched)
	self:initHelpers()
end

function Shared:OnDisable()
	EventManager:RemoveListener(EventTypes.System_ContextSwitched, Shared.OnContextSwitched)
	EventManager:RemoveListener(EventTypes.System_BeforeContextSwitched, Shared.OnBeforeContextSwitched)
end

-- 初始化辅助相关
function Shared:initHelpers()
	self.CommonCoverRoot:Active(true, function ()
		Helper.AddUserDefinedRoot('FloatingTip')
    end)
    
    self.CommonPopupRoot:Active(true, function()
        Helper.AddCommonPopupRoot('CommonDialog')
    end)
end

-- 重置辅助功能
function Shared:resetHelpers()
    self.CommonCoverRoot:Deactive(true, true)
    self.CommonPopupRoot:Deactive(true, true)
end

-- context切换时接收处理
function Shared.OnContextSwitched(context)
	print('[Shared] OnContextSwitched -------')
    Shared.CommonCoverRoot:Active(true)
    Shared.CommonPopupRoot:Active(true)
end

-- context切换之前收到消息
function Shared.OnBeforeContextSwitched()
	print('[Shared] OnBeforeContextSwitched -------')
	Shared:resetHelpers()
end

return Shared
