local barView = {}

barView.args = {
	btnOpen = Types.Button,
	btnClose = Types.Button

}

local args = barView.args

function barView:Awake()
	
end

function barView:Start()
	args.btnClose.onClick:AddListener(
		Animator.Play("StateName")
	)
	args.btnOpen.onClick:AddListener(

	)
end

function barView:OnEnable()
end

function barView:OnDisable()
end

return barView