--[[
    场景加载切换时的载入画面
]]

local CommonLoadingView = {}

CommonLoadingView.args = {
	sliderProgress = Types.Slider,
	txtTip = Types.Text,
}

local args = CommonLoadingView.args

function CommonLoadingView:Awake()
	CommonLoadingView.Progress(0)
end

function CommonLoadingView:OnEnable()
	EventManager:AddListener(EventTypes.System_TaskProgress, CommonLoadingView.Progress)
end

function CommonLoadingView:OnDisable()
	EventManager:RemoveListener(EventTypes.System_TaskProgress, CommonLoadingView.Progress)
end

function CommonLoadingView.Progress(progress)
	print('[CommonLoadingView] progress ' .. progress * 100)
	args.sliderProgress.value = progress
	args.txtTip.text = string.format("loading percent %d%%", math.floor(progress * 100))
end

return CommonLoadingView