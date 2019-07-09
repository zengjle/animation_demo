--[[
]]

local gameBattleView = {}

gameBattleView.args = {
    textTitle = Types.Text,
    btnJump = Types.Button,
    preJumkMan = Types.GameObject
}

local args = gameBattleView.args
local _pladPlayableDir
local _tDirsctor = {}
function gameBattleView:Awake()
    _pladPlayableDir = self.gameObject:GetComponent("PlayableDirector")
--     -- Config.utils.print_r(_pladPlayableDir.playableAsset.outputs)
--     local pladOutput = _pladPlayableDir.playableAsset.outputs
    print(_pladPlayableDir)
--     print(pladOutput:GetEnumerator())
--     print(pladOutput:MoveNext())
--     while pladOutput:MoveNext() do
--     print(pladOutput.Current.streamName)
--     _tDirsctor[pladOutput.Current.streamName] = pladOutput.Current
-- end
    -- for k,v in pairs(_pladPlayableDir.playableAsset.outputs) do
    --     _tDirsctor[k] = v
    -- end
end

function gameBattleView:Start()
    -- EventManager:AddListener('fuck',function()
    --     args.textTitle.text = "fuck!!!"
    -- end)
    EventManager:RemoveListener('Timeline',gameBattleView.eventCallBack)
    EventManager:AddListener('Timeline',gameBattleView.eventCallBack)
    -- local jumkMan = GameObject.Instantiate(args.preJumkMan,Vector3(0,0,0),CS.UnityEngine.Quaremion.identity,self.transform)
    -- _pladPlayableDir.SetGenericBinding(_tDirsctor["jumpTrack"].sourceObject,jumkMan)
    -- _pladPlayableDir.SetGenericBinding(_tDirsctor["titleTrack"].sourceObject,jumkMan)
    -- Comon:testFun("啊啊啊啊啊啊啊啊啊啊")
    -- Config.utils.print_r(Comon)
    -- Config.utils.print_r(self)
    print(args.preJumkMan)
    print(Vector3(0,0,0))
    print(UnityEngine.Quaremion.identity)
    print(self.gameObject.transform)
    local objJumkMan = GameObject.Instantiate(args.preJumkMan,Vector3(0,0,0),UnityEngine.Quaremion.identity,self.gameObject.transform)
    print(objJumkMan)
    Comon:initBindingDict(_pladPlayableDir)
    args.btnJump.onClick:AddListener(function ()
        Comon:setPlayableBinding("jumpTrack",objJumkMan)
        Comon:setPlayableBinding("playerTrack",objJumkMan)
        Comon:play()
    end)

end

function gameBattleView.eventCallBack (str)
    if gameBattleView.controller.actived == true then
        print("uiRoleView:true")
        gameBattleView.controller:Deactive()
    else
        print("uiRoleView:false")
        gameBattleView.controller:Active()
    end
end

function gameBattleView:OnEnable()
end

function gameBattleView:OnDisable()
    Comon:ClearGenericBinding("jumpTrack")
    Comon:ClearGenericBinding("playerTrack")
end

return gameBattleView