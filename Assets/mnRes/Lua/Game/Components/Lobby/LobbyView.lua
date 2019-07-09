--[[
 --
--                        _oo0oo_
--                       o8888888o
--                       88" . "88
--                      (| -_-  |)
--                      0\  =  /0
--                    ___/`---'\___
--                  .' \\|     |-- '.
--                 / \\|||  :  |||-- \
--                / _||||| -:- |||||- \
--               |   | \\\  -  --/ |   |
--               | \_|  ''\---/''  |_/ |
--               \  .-\__  '-'  ___/-. /
--             ___'. .'  /--.--\  `. .'___
--          ."" '<  `.___\_<|>_/___.' >' "".
--         | | :  `- \`.;`\ _ /`;.`/ - ` : | |
--         \  \ `_.   \_ __\ /__ _/   .-` /  /
--     =====`-.____`.___ \_____/___.-`___.-'=====
--                       `=---='
--
--
--     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
--
--               佛祖保佑         永无BUG
--               阿弥陀佛         性能无忧
--
]]

local LobbyView = {}

LobbyView.args = {
    btnBackToLogin = Types.Button,
    btnAnimation = Types.Button,
    btnTimeline = Types.Button,
    imgBackGround = Types.Image
}

local args = LobbyView.args

function LobbyView:Awake()
    args.btnBackToLogin.onClick:AddListener(function ()
        self.controller:BackToLogin()
    end)
    args.btnAnimation.onClick:AddListener(function ()
        print("btnAnimation!!!!!!!!!!!!!")
        EventManager:Broadcast('Animation',"你看我青龙偃月!!!!!!!!!")
        EventManager:Broadcast('Timeline',"你看我青龙偃月!!!!!!!!!")
    end)
    args.btnTimeline.onClick:AddListener(function ()
        print("btnTimeline!!!!!!!!!!!!!")
        EventManager:Broadcast('Animation',"你看我青龙偃月!!!!!!!!!")
        EventManager:Broadcast('Timeline',"你看我青龙偃月!!!!!!!!!")
    end)
    -- local imgBg = UnityEngine.Resources:Load("house_bg")
    -- print("LLLLLLLLLLLLLLLLLLLLoad"..imgBg)
    -- args.imgBackGround.sprite = imgBg



end

function LobbyView:Start()
    local uiRoleCom = GetComponent("UI_ROLE")
    uiRoleCom:Deactive()
end

function LobbyView:OnEnable()
end

function LobbyView:OnDisable()
end

return LobbyView