--[[
 登录界面
]]

local uiRole = {}

uiRole.args = {
    textTitle = Types.Text,
    jumkMan = Types.GameObject,
    btnsStateChange = Types.Buttons
}
local _aniJumkMan
local args = uiRole.args

function uiRole:Awake()
end

function uiRole:Start()
    _aniJumkMan = args.jumkMan:GetComponent("Animator")
    EventManager:RemoveListener('Animation',uiRole.eventCallBack)
    EventManager:AddListener('Animation',uiRole.eventCallBack)
    --[[
        args.btnsStateChange[i]
        i = 0 --攻击按钮
        i = 1 --休息按钮
        i = 2 --受伤按钮
    --]]
    args.btnsStateChange[0].onClick:AddListener(function ()
        args.textTitle.text = "攻击!!!!!!!!"
        _aniJumkMan:SetTrigger("attack")
    end)

    args.btnsStateChange[1].onClick:AddListener(function ()
        args.textTitle.text = "休息呼呼呼!!!!!!!!"
        _aniJumkMan:SetTrigger("normal")
    end)
    args.btnsStateChange[2].onClick:AddListener(function ()
        args.textTitle.text = "受伤啊啊啊!!!!!"
        _aniJumkMan:SetTrigger("hurt")
    end)
    -- print(Config.utils.serialize(self))
    --print(self.gameObject.transform:Find("RoleList/Viewport/Content").childCount)
    -- local content = self.gameObject.transform:Find("RoleList/Viewport/Content")
    -- print("////////////////////////ssssss"..content)
end

function uiRole.eventCallBack (str)
    print("fuck!!!"..str) 
    if uiRole.controller.actived == true then
        print("uiRoleView:true")
        uiRole.controller:Deactive()
    else
        print("uiRoleView:false")
        uiRole.controller:Active()
    end
end

function uiRole:OnEnable()
end

function uiRole:OnDisable()
    print("removeListener")
end

return uiRole