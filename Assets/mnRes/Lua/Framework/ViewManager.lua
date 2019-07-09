--[[
视图管理
]]

ViewManager = {}

local self = ViewManager

self.root = nil
self.canvasList = {}
self.resBaseDir = "Assets/mnRes/Prefabs/UI/"
self.contexts = {}
self.current = nil
self.disabled = nil
self.next = nil
self.lastFocusView = nil
self.popupIndex = 0
self.cameraUI = nil

local signature = '__view_signature__'
local elementSignature = '__element_view__'
local popupLayer = 'Popup'

-- 初始化
function ViewManager.Initialize()
    print('[ViewManager] Initialize')

    local uiPrefab = ResourceManager:LoadAsset(self.resBaseDir .. 'UI.prefab')
    local root = GameObject.Instantiate(uiPrefab)
    local canvasTempl = root.transform:GetComponentInChildren(typeof(Canvas))
    local cameraUI = root.transform:GetComponentInChildren(typeof(Camera))

    -- SharedManager.cameraUI = cameraUI
    self.cameraUI = cameraUI

    root.name = 'UI'
    DontDestroyOnLoad(root)
    self.root = root

    for i = 1, #Config.ViewLayer.configs do
        local li = Config.ViewLayer.configs[i]
        local canvas = GameObject.Instantiate(canvasTempl)

        canvas.name = li.name
        canvas.transform:SetParent(root.transform, false)
        canvas.transform.localPosition = UnityEngine.Vector2.zero
        canvas.sortingOrder = li.order
        canvas.gameObject:SetActive(true)

        if li.name == popupLayer then
            self.popupIndex = i
        end

        self.canvasList[i] = canvas.transform
    end

    canvasTempl.gameObject:SetActive(false)

    self.disabled = canvasTempl.gameObject.transform

    -- 初始化Loading相关的
    local lname = Config.ViewConstants.loadingContext
    local context = LoadingContext.New(lname)

    self.contexts[lname] = context
    context:OnEnable()
end

-- 清理
function ViewManager.Shutdown()
    print('[ViewManager] Shutdown')
end

-- 创建Context
function ViewManager.CreateContext(name, path, root, active, overwrited)
    local context = self.contexts[name]

    -- 已经存在，并不打算重新创建，直接返回
    if context and overwrited then
        self.DeleteContext(name)
        context = nil
        self.contexts[name] = nil
    end

    -- 不存在，重新创建
    if not context then
        context = Context.New(name, path, root)
        self.contexts[name] = context
    end

    -- 激活
    if not self.current or active then
        self.SwitchContext(name)
    end
end

-- 删除Context
function ViewManager.DeleteContext(name)
    local context = self.contexts[name]
    if not context then
        return
    end

    context:OnDisable()
    context:OnDestroy()

    if context == self.current then
        self.current = nil
    end
end

-- 切换Context
function ViewManager.SwitchContext(name)
    local context = self.contexts[name]
    if not context then
        warn('[ViewManager] active not existed context ' .. name)
        return
    end

    if context == self.current then
        return
    end

    self.lastFocusView = nil

    EventManager:Broadcast(EventTypes.System_BeforeContextSwitched, self.current)

    -- 如果当前context为空，则假设场景已经被切换，则不启动loading切换，而是直接激活context
    if not self.current then
        self.current = context
        context:OnEnable()
        return
    end

    self.current:OnDisable()

    self.current = context
    context:OnEnable()

    EventManager:Broadcast(EventTypes.System_ContextSwitched, context)
end

function ViewManager.SetNextContext(context)
    self.next = context
end

-- 获取context
function ViewManager.GetContext(name)
    local context = self.contexts[name]
    if not context then
        error('[ViewManager] fail to get context ' .. name)
        return
    end

    return context
end

-- 当前Context是否为指定名字
function ViewManager.IsCurrentContext(name)
    if not self.current then
        return false
    end

    return self.current.name == name
end

-- 根据路径创建view
function ViewManager.CreateView(path, sync, tag, controller, handler)
    print("try to load " .. path)
    local assetName = self.resBaseDir .. path .. '.prefab'
    local onFinished = function(prefab)
        print("finish load " .. path)
        if not prefab then
            error('[ViewManager] create view on fail to load asset ' .. assetName)
            return
        end

        local view = {}

        view.go = GameObject.Instantiate(prefab)
        view.__lua_target__ = view.go:GetComponent(typeof(LuaTarget))
        view.name = path
        view.__signature__ = signature
        view.tag = tag
        view.controller = controller

        if view.__lua_target__ and not view.__lua_target__.Valid then
            view.go:SetActive(true)

            if not view.__lua_target__.Table then
                error('[ViewManager] view has invalid \"luaFilename of LuaTarget\", path = ' .. path)
                return
            end

            view.__lua_target__.Table.controller = controller
            view.go:SetActive(false)
        elseif view.__lua_target__ then
            view.__lua_target__.Table.controller = controller
        end

        if handler and type(handler) == 'function' then
            handler(view)
        end
    end

    -- 同步或异步载入
    if sync then
        ResourceManager:LoadAsset(assetName, onFinished)
    else
        ResourceManager:LoadAssetAsync(assetName, onFinished)
    end
end

function ViewManager.CreateViewByPrefab(prefab, isInstance, controller, tag)
    local view = {}

    view.go = isInstance and prefab or GameObject.Instantiate(prefab)
    view.__lua_target__ = view.go:GetComponent(typeof(LuaTarget))
    view.name = path
    view.__signature__ = signature
    view.tag = tag
    view.controller = controller

    if view.__lua_target__ and not view.__lua_target__.Valid then
        view.go:SetActive(true)
        view.__lua_target__.Table.controller = controller
        view.go:SetActive(false)
    elseif view.__lua_target__ then
        view.__lua_target__.Table.controller = controller
    end

    return view
end

-- 重置view和controller的关系
function ViewManager.ResetView(view, controller)
    if not view or view.__signature__ ~= signature then
        return
    end

    if view.go.activeSelf then
        view.go:SetActive(false)
    end

    view.__lua_target__.Table.controller = controller

    if controller then
        view.go:SetActive(true)
    end

    return view
end

-- 载入ui prototype，可以理解为载入一个prefab，方便于重复创建gameObject
function ViewManager.LoadViewPrototype(path, sync, handler)
    local assetName = self.resBaseDir .. path .. '.prefab'

    local onFinished = function (prefab)
        if not prefab then
            error('[ViewManager] fail to load asset ' .. assetName)
            return
        end

        if handler then
            handler(prefab)
        end
    end

    -- 同步或异步载入
    if sync then
        ResourceManager:LoadAsset(assetName, onFinished)
    else
        ResourceManager:LoadAssetAsync(assetName, onFinished)
    end
end

-- 压入一个View到当前Context，堆栈结构管理
function ViewManager.PushView(view, layer, anim)
    if not view or view.__signature__ ~= signature then
        error('[ViewManager] push view is invalid')
        return
    end

    local context = self.current

    -- 如果loading组件，加入loading context，如果当前是loading context, 而tag不是loading组件，则加到有效的next context中去
    if view.tag == Config.ViewConstants.tagLoading then
        context = self.contexts[Config.ViewConstants.loadingContext]
    elseif context == self.contexts[Config.ViewConstants.loadingContext] and self.next then
        context = self.next
    end

    if not context then
        error('[ViewManager] push current context is invalid')
        return
    end

    local canvas = self.canvasList[layer]
    if not canvas then
        error('[ViewManager] cannot found find layer ' .. layer .. ' on push view ' .. view.name)
        return
    end

    view.go.transform:SetParent(canvas, false)
    context:PushView(view, layer, anim)

    -- 如果loading组件，东西都准备好了，激活loading context
    if view.tag == Config.ViewConstants.tagLoading then
        self.SwitchContext(Config.ViewConstants.loadingContext)
    end

    if layer < self.popupIndex then
        self.tryViewFocusChanged(view)
    end
end

-- 弹出view，如果指定view，则弹出view， 如果没有指定，则弹出最上层的view
function ViewManager.PopView(view, layer, anim, ignoreTop)
    if view and view.__signature__ ~= signature then
        error('[ViewManager] pop view is invalid')
        return
    end

    local context = self.current

    if view.tag == Config.ViewConstants.tagLoading then
        context = self.contexts[Config.ViewConstants.loadingContext]
    elseif context == self.contexts[Config.ViewConstants.loadingContext] and self.next then
        context = self.next
    end

    if not context then
        error('[ViewManager] pop current context is invalid')
        return
    end

    view.go.transform:SetParent(self.disabled, false)

    context:PopView(view, layer, anim, ignoreTop)

    if layer < self.popupIndex then
        local topView = context:GetTopView(self.popupIndex-1)
        self.tryViewFocusChanged(topView)
    end
end

function ViewManager.tryViewFocusChanged(view)
    if view ~= self.lastFocusView then
        if self.lastFocusView then
            EventManager:Broadcast(EventTypes.System_ViewOutFocus, self.lastFocusView.controller.tag)
        end

        self.lastFocusView = view

        if self.lastFocusView then
            EventManager:Broadcast(EventTypes.System_ViewInFocus, self.lastFocusView.controller.tag)
        end
    end
end

function ViewManager.GenElementView(go, path, controller)
    if not go then
        return nil
    end

    local view = {}

    view.go = go
    view.__lua_target__ = view.go:GetComponent(typeof(LuaTarget))
    view.name = path
    view.__signature__ = elementSignature
    view.controller = controller

    if view.__lua_target__ and not view.__lua_target__.Valid then
        view.go:SetActive(true)
        view.__lua_target__.Table.controller = controller
        view.go:SetActive(false)
    elseif view.__lua_target__ then
        view.__lua_target__.Table.controller = controller
    end

    return view
end

function ViewManager.GenElementViewFromTable(lt, path, controller)
    if not lt then
        return nil
    end

    local view = {}

    view.go = lt.gameObject
    view.__lua_target__ = lt
    view.name = path
    view.__signature__ = elementSignature
    view.controller = controller

    if view.__lua_target__ and not view.__lua_target__.Valid then
        view.go:SetActive(true)
        view.__lua_target__.Table.controller = controller
        view.go:SetActive(false)
    elseif view.__lua_target__ then
        view.__lua_target__.Table.controller = controller
    end

    return view
end

-- Accessor
function ViewManager.GenAccessor(go, tag)
    if not go then
        return nil
    end

    local lt = nil
    if tag then
        local lts = go:GetComponents(typeof(LuaTarget))
        for i = 0, lts.Length-1 do
            if string.ends(string.stripExtension(lts[i].luaFilename), tag) then
                lt = lts[i]
                break
            end
        end
    else
        lt = go:GetComponent(typeof(LuaTarget))
    end

    if not lt then
        return nil
    end

    if not lt.Valid then
        go:SetActive(true)
    end

    return lt.Table
end

function ViewManager.OnSceneFinished(name)
    EventManager:Broadcast(EventTypes.System_SceneLoadFinished, name)
end

-- 设置UI状态
function ViewManager.SetCameraState(enable)
    self.cameraUI.gameObject:SetActive(enable)
end

-- 根据名字获取所在层
function Name2ViewLayer(name)
    return Config.ViewLayer[name].id
end