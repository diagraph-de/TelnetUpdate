#!/usr/bin/lua

function readStatus()
    local inp = io.open("/tmp/status", "r")
    if nil == inp then
        return nil
    end
    local data = inp:read("*all")
    inp:close()
    local lines = {}
    for x in string.gfind(data, "([^\n]+)") do
        table.insert(lines, x)
    end
    return lines
end

function findString(lines, str)
    if nil == lines then
        return nil
    end
    for i, x in ipairs(lines) do
        if string.find(x, str) then
            return x
        end
    end
    return nil
end

function pTask_getData(lines, i)
    buf = findString(lines, "ifc" .. tostring(i))
    if nil ~= buf then
        x = {nil, nil, nil, nil, nil, nil, nil, nil, nil}
        for t in string.gfind(buf, "([^\t]+)") do
            if nil ~= tonumber(t) then
                table.insert(x, tonumber(t))
            else
                table.insert(x, t)
            end
        end
        if x[9] == nil then
            x[9] = ''
        end
        return x[2], x[3], x[4], x[5], x[6], x[7], x[8], x[9]
    end
    return nil, nil, nil, nil, nil, nil, nil, nil
end

function getIDS(lines, i)
    local buf = findString(lines, "ids" .. tostring(i))
    if nil ~= buf then
        local x = {nil, nil, nil, nil, nil, nil, nil, nil}
        for t in string.gfind(buf, "([^\t]+)") do
            if nil ~= tonumber(t) then
                table.insert(x, tonumber(t))
            end
        end
        return x[1], x[2], x[3], x[4], x[5], x[6]
    end
    return nil, nil, nil, nil, nil, nil, nil, nil
end

function getItm(lines, match)
    local x = ''
    if nil ~= lines then
        x = string.match(string.gsub(findString(lines, match), match, ""), "^%s*(.-)%s*$")
    end
    if x == nil then
        x = ""
    end
    return x
end

function getUptime()
    local inp = io.open("/proc/uptime", "r")
    local up = nil
    if nil ~= inp then
        local time = math.floor(tonumber(inp:read("*number")))
        inp:close()
        up = os.date("%m/%d/%Y %T", os.time() - time)
    end
    return up
end

function getKrnl_FS_version()
    local inp = io.open("/tmp/versions", "r")
    local data = ""
    if nil ~= inp then
        data = inp:read("*all")
        inp:close()
    else
        data = "Kernel: "
        data = data .. io.popen("uname -rv"):read()
        data = data .. "FileSystem: "
        data = data .. io.popen("cat /etc/fs_version"):read()
    end
    return data
end

function replaceHTMLTags(data)
    -- Replace all occurrences of "</br>" or "\n" with ""
    data = string.gsub(data, "</br>", "")
    data = string.gsub(data, "\n", "")
    data = string.gsub(data, "FileSystem:", " FileSystem:")
    return data
end

function encode_json(obj)
    local json = ""
    local function recurse(obj)
        if type(obj) == "number" then
            json = json .. tostring(obj)
        elseif type(obj) == "string" then
            json = json .. string.format("%q", obj)
        elseif type(obj) == "table" then
            json = json .. "{"
            for k, v in pairs(obj) do
                json = json .. string.format('"%s":', k)
                recurse(v)
                json = json .. ","
            end
            json = json:sub(1, -2)
            json = json .. "}"
        elseif type(obj) == "boolean" then
            json = json .. (obj and "true" or "false")
        else
            json = json .. "null"
        end
    end
    recurse(obj)
    return json
end

function main()
    local output = {}
    local controllerStatus = {}
    local task = {}
    local idsInfo = {}

    -- Populate controllerStatus
    controllerStatus["last_boot"] = getUptime()
    controllerStatus["ram_used"] = getItm(readStatus(), "ram")
    controllerStatus["flash_used"] = getItm(readStatus(), "flash")
    controllerStatus["input_status"] = getItm(readStatus(), "input")
    controllerStatus["relay_status"] = getItm(readStatus(), "output")
    controllerStatus["pm_counter"] = getItm(readStatus(), "pmcount")

    -- Populate pTask
    for i = 0, 1 do
        local card, fpga, detect, speed, cnt, pause, PEL, name = pTask_getData(readStatus(), i)
        if card and fpga and detect and speed and cnt and pause and PEL and name then
            task["card"] = card
            task["fpga"] = fpga
            task["detect"] = detect
            task["speed"] = speed
            task["cnt"] = cnt
            task["pause"] = pause
            task["PEL"] = PEL
            task["name"] = name
        end
    end

    -- Populate idsInfo
    for i = 0, 1 do
        local maj, min, status, p, v, brok = getIDS(readStatus(), i)
        if maj and min and status and p and v and brok then
            local ids = {}
            ids["version"] = string.format("%d Ver. %d.%d", i, maj, min)
            ids["status"] = status
            ids["press"] = p
            ids["vac"] = v
            ids["broken"] = brok
            table.insert(idsInfo, ids)
        end
    end

    -- Populate the output table
    output["controllerStatus"] = controllerStatus
    output["task"] = task
    output["idsInfo"] = idsInfo
    output["current_time"] = os.date("%H:%M:%S")
    output["current_date"] = os.date("%m/%d/%Y")
    output["version"] = replaceHTMLTags(getKrnl_FS_version())

    print("Content-Type: application/json")
    print("Cache-Control: max-age=3600")
    print("Access-Control-Allow-Origin: *")
    print("ETag: \"abc123\"")
    print("Content-Length: " .. string.len(encode_json(output)))

    print("")

    print(encode_json(output))
end

main()
