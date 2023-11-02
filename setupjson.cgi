#!/usr/bin/lua

local configPath = "/home/inkjet/cfg/setup.cfg"

local f = io.open(configPath, "r")

if f then
    local xml_data = f:read("*a")
    f:close()

    xml_data = xml_data:gsub("</cfg>", "")
    xml_data = xml_data:gsub(">", " />", 1)

    local function xml_to_keyvalue(xml)
        local keyvalue = {}

        local function appendKey(key, index)
            if index > 1 then
                return key .. index
            else
                return key
            end
        end

        local tag_count = {}
        local current_daisychain = ""
        local com_count = 0
        local shift_count = 0
        local dasychain_count = 0
        local encoder_count = 0
        local options_count = 0
        local ph_count = 0

        for tag, attributes in xml:gmatch('<(%w+)(.-)>') do
            local entry = {}
            local tag_key = appendKey(tag, tag_count[tag] or 0)

            for key, value in attributes:gmatch('(%w+)="([^"]+)"') do
                entry[key] = value
            end

            if tag == "daisychain" then
                dasychain_count = dasychain_count + 1
                tag_key = "daisychain_" .. dasychain_count
                current_daisychain = tag_key
            end

            if tag == "options" then
                options_count = options_count + 1
                tag_key = "options_" .. options_count
            end

            if tag == "encoder" then
                encoder_count = encoder_count + 1
                tag_key = "encoder_" .. encoder_count
            end

            if tag == "shift" then
                shift_count = shift_count + 1
                tag_key = "shift_" .. shift_count
            end

            if tag == "com" then
                com_count = com_count + 1
                tag_key = "com_" .. com_count
            end

            if tag == "ph" then
                ph_count = ph_count + 1
                tag_key = "ph_" .. ph_count .. "_" .. current_daisychain
            end

            if not keyvalue[tag_key] then
                keyvalue[tag_key] = entry
            else
                if type(keyvalue[tag_key]) == "table" then
                    table.insert(keyvalue[tag_key], entry)
                else
                    local temp = keyvalue[tag_key]
                    keyvalue[tag_key] = {temp, entry}
                end
            end

            -- Update tag count
            tag_count[tag] = (tag_count[tag] or 0) + 1
        end

        return keyvalue
    end

    local keyvalue_data = xml_to_keyvalue(xml_data)

    local function keyvalue_to_json(keyvalue)
        local json_str = "{"

        -- Sort keys alphabetically
        local sorted_keys = {}
        for key, _ in pairs(keyvalue) do
            table.insert(sorted_keys, key)
        end
        table.sort(sorted_keys)

        local keyvalue_pairs = {}
        for _, key in ipairs(sorted_keys) do
            local value = keyvalue[key]
            local entry = {}
            if type(value) == "table" then
                for k, v in pairs(value) do
                    if type(v) == "table" then
                        local sub_entries = {}
                        for _, sub_v in ipairs(v) do
                            local sub_entry = {}
                            for sub_k, sub_sub_v in pairs(sub_v) do
                                sub_entry[#sub_entry + 1] = '"' .. sub_k .. '":"' .. sub_sub_v .. '"'
                            end
                            sub_entries[#sub_entries + 1] = "{" .. table.concat(sub_entry, ",") .. "}"
                        end
                        entry[#entry + 1] = '"' .. k .. '":[' .. table.concat(sub_entries, ",") .. "]"
                    else
                        entry[#entry + 1] = '"' .. k .. '":"' .. v .. '"'
                    end
                end
            else
                entry[#entry + 1] = '"' .. key .. '":"' .. value .. '"'
            end
            keyvalue_pairs[#keyvalue_pairs + 1] = '"' .. key .. '":{' .. table.concat(entry, ',') .. '}'
        end

        json_str = json_str .. table.concat(keyvalue_pairs, ',') .. "}"
        return json_str
    end

    local json_data = keyvalue_to_json(keyvalue_data)

    io.write("Content-Type: application/json\r\n")
    io.write("Cache-Control: max-age=3600\r\n")
    io.write("Access-Control-Allow-Origin: *\r\n")
    io.write("ETag: \"abc1234\"\r\n")
    io.write("Content-Length: " .. #json_data .. "\r\n")
    io.write("\r\n")
    io.write(json_data)    
    io.write("\r\n")
end
