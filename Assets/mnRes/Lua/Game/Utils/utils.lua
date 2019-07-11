local utils = {}

utils.get_time_string = function (ts)
    local check_time = function (i) 
        if i < 10 then
            i = "0" .. i
        end
        return i
    end
    --计算剩余的小时数
    local hh = math.floor( ts / 60 / 60 )
    --计算剩余的分钟数
    local mm = math.floor( ts / 60 % 60 )
    --计算剩余的秒数
    local ss = math.floor( ts % 60 )
    return check_time(hh) .. ":" .. check_time(mm) .. ":" .. check_time(ss)
    
end

utils.parse_num = function (num) 
    num = tonumber(num);

    if (num >= 100000000) then
        local val = num / 100000000;
        num = string.format("%.3f亿",val);
    end

    return num;
    
end


return utils