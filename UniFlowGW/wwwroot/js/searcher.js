var Column = function(obj) { $.extend(this,{type:'VARCHAR(45)',isnull:true},obj); }
Column.prototype.isText = function() { return /CHAR|TEXT/i.test(this.type); }
Column.prototype.isInt = function() { return /INT/i.test(this.type); }
Column.prototype.isNumber = function() { return /INT|FLOAT|DOUBLE|REAL|DECIMAL/i.test(this.type); }
Column.prototype.isDate = function() { return 'DATE' == this.type.toUpperCase(); }
Column.prototype.isDateTime = function() { return /DATE|TIME/i.test(this.type); }
Column.prototype.isBinary = function() { return /BINARY|BLOB/i.test(this.type); }
var Schema = function(obj) {
	$.extend(this,{columns:[],pk:[]},obj);
	var colMap = {};
	$.each(this.columns, function(i,c) {
		colMap[c.name] = new Column(c);
	})
	this.colMap = colMap;
}
Schema.prototype.col = function(name) { return this.colMap[name]; }
var SearchConfig = function(obj) {
	$.extend(this,{mode:"DEFAULT",items:[]},obj);
	var map = {};
	$.each(this.items, function(i,c) {
		map[c.key] = c;
	})
	this._map = map;
}
SearchConfig.prototype.hasOptList = function(key) {
	return key in this._map && !$.isEmptyObject(this._map[key].optList);
}
SearchConfig.prototype.isTags = function(key) {
	return key == "tags" && this.hasOptList(key);
}
SearchConfig.prototype.isSearchable = function(key) {
	return !(key in this._map && this._map[key].searchable);
}
SearchConfig.prototype.optListOf = function(key) {
	return this._map[key].optList;
}
var ViewConfig = function(obj) {
	$.extend(this, {items:[]}, obj);
	var map = {};
	$.each(this.items, function(i,c) {
		map[c.key] = c;
	})
	this._map = map;
}
ViewConfig.prototype.headerOf = function(key) {
	return (key in this._map && this._map[key].header) ?
			this._map[key].header : key;
}
ViewConfig.prototype.textOf = function(key, value) {
	if (key in this._map && this._map[key].optList) {
		var list = this._map[key].optList;
		if (value in list) return list[value];
	}
	return value;
}

var Searcher = function(div, schema, searchConfig, viewConfig, queryList) {
	this.$div = $(div);
	this.schema = schema;
	this.searchConfig = searchConfig;
	this.viewConfig = viewConfig;
	this.nextNo = 0;

	var self = this;

	var $div = this.$div;

	$.each(queryList, function(i,item) {
		self.addQuery(item.key, item.op, item.val);
	});

	var $divbtn = $('<div class="btn-group">').insertAfter($div);
	var $btn = $('<a class="btn btn-primary dropdown-toggle" data-toggle="dropdown" href="javascript:;">')
		.appendTo($divbtn);
    $btn.append('<i class="glyphicon glyphicon-plus"> </i> 添加条件 <span class="caret"></span>');
	var $ul = $('<ul class="dropdown-menu">').appendTo($divbtn);

	$.each(schema.colMap, function(key, column) {
		if (searchConfig.isSearchable(key)) return;

		if (searchConfig.isTags(key)) {
			var $li = $('<li></li>').appendTo($ul);
			$('<a href="javascript:;">' + viewConfig.headerOf(key) + '</a>').appendTo($li)
				.click(function(){self.addTagsQuery(key)});
			var $subul = $('<ul class="dropdown-menu"></ul>').appendTo($li);

			return;
		}
		
		var submenu = [];
		if (searchConfig.hasOptList(key)) {
			submenu.push({ text: "选项", act: function(){self.addOptListQuery(key);} });
			submenu.push({ text: "多选", act: function(){self.addOptMultiQuery(key);} });
		}
		if (column.isText()) {
			submenu.push({ text: "匹配", act: function(){self.addTextQuery(key, "STR_MATCH");} });
			submenu.push(false);
			submenu.push({ text: "为", act: function(){self.addTextQuery(key, "EQ");} });
			submenu.push({ text: "开始为", act: function(){self.addTextQuery(key, "STR_BEGIN");} });
			submenu.push({ text: "末尾为", act: function(){self.addTextQuery(key, "STR_END");} });
			submenu.push(false);
			submenu.push({ text: "正则表达式匹配", act: function(){self.addTextQuery(key, "STR_REGEX");} });
		}
		if (column.isNumber()) {
			submenu.push({ text: "等于", act: function(){self.addNumQuery(key, "EQ");} });
			submenu.push({ text: "至少为", act: function(){self.addNumQuery(key, "GE");} });
			submenu.push({ text: "最大为", act: function(){self.addNumQuery(key, "LE");} });
			submenu.push(false);
			submenu.push({ text: "文字匹配", act: function(){self.addTextQuery(key, "STR_MATCH");} });
		}
		if (column.isDateTime()) {
			submenu.push({ text: "等于", act: function(){self.addDateQuery(key, "EQ");} });
			submenu.push({ text: "日期不晚于", act: function(){self.addDateQuery(key, "GE");} });
			submenu.push({ text: "日期不早于", act: function(){self.addDateQuery(key, "LE");} });
			submenu.push(false);
			submenu.push({ text: "文字匹配", act: function(){self.addTextQuery(key, "STR_MATCH");} });
		}
		if (submenu.length == 0) return;

		var $li = $('<li class="dropdown dropdown-submenu"></li>').appendTo($ul);
		$('<a href="javascript:;">' + viewConfig.headerOf(key) + '</a>').appendTo($li)
			.click(submenu[0].act);
		var $subul = $('<ul class="dropdown-menu"></ul>').appendTo($li);

		$.each(submenu, function(idix, item) {
			if (!item) {
				$('<li class="divider"></li>').appendTo($subul);
				return;
			}
			var $subli = $('<li></li>').appendTo($subul);
			$('<a href="javascript:;">' + item.text + '</a>').appendTo($subli).click(item.act);
		});
	});
}
Searcher.prototype.addQuery = function(key, op, val) {
	var column = this.schema.col(key);

	if (op == "TAGS") return this.addTagsQuery(key, op, val);
	if (op == "IS") return this.addOptListQuery(key, op, val);
	if (op == "IN") return this.addOptMultiQuery(key, op, val);
	if ($.inArray(op, ["EQ", "LE", "GE"]) != -1) {
		if (column.isNumber()) return this.addNumQuery(key, op, val);
		if (column.isDateTime()) return this.addDateQuery(key, op, val);
	}
	return this.addTextQuery(key, op, val);
}
Searcher.prototype.addQueryCommon = function(key, name, op, idkey, idop, idval) {
    var $row = $('<div class="form-group">').css({ display: "block", marginBottom: "5px", marginTop: "5px" }).appendTo(this.$div);
	$('<input type="hidden">').attr('id', idkey).val(key).appendTo($row);
	$('<input type="hidden">').attr('id', idop).val(op).appendTo($row);
    $('<a href="javascript:;" onclick="$(this).parent().remove()"><i class="glyphicon glyphicon-minus">&nbsp;</i></a>').appendTo($row);
	$row.append(" ");
	$('<label>').attr('for', idval).text(name).appendTo($row);
	$row.append(" ");
	return $row;
}
Searcher.prototype.addQueryOpMenu = function($row, op, idop, opmenu, opnames) {
    var $opdiv = $('<div class="input-group-btn">').appendTo($row);
	var $btn = $('<a class="btn btn-default dropdown-toggle" data-toggle="dropdown" href="javascript:;">').appendTo($opdiv);
    var $opname = $('<span>').text(opnames[op]).appendTo($btn);
    $btn.append(' ').append('<span class="caret">');
	var $menu = $('<ul class="dropdown-menu">').appendTo($opdiv);
	$.each(opmenu, function(idx, t) {
		if (!t) {
			$('<li class="divider">').appendTo($menu); return;
		}
		var $li = $('<li>').appendTo($menu);
		var $a = $('<a href="javascript:;">').text(t.text).appendTo($li)
		.click(function() {
			$('#' + idop).val(t.op);
			$opname.text(opnames[t.op]);
		});
	});
}
Searcher.prototype.addOptListQuery = function(key, op, value) {
	if (!this.searchConfig.hasOptList(key)) return;

	op = op || "IS";
	if (op != "IS") return;

	var idkey = "qkey_" + this.nextNo,
		idop = "qop_" + this.nextNo,
		idval = "qval_" + this.nextNo;
	this.nextNo ++;
	var $row = this.addQueryCommon(key, this.viewConfig.headerOf(key), op, idkey, idop, idval);

    var $div = $('<div class="input-group">').appendTo($row);

	$('<span class="input-group-addon">').text("为").appendTo($div);
	var $sel = $('<select>').attr('id', idval).addClass('form-control').appendTo($div);
	var optList = this.searchConfig.optListOf(key);
	$.each(optList, function(opt, text) {
		var $opt = $('<option>').val(opt).text(text).appendTo($sel);
		if (value == opt) $opt.attr('selected', 'selected');
	});
}
Searcher.prototype.addOptMultiQuery = function(key, op, value) {
	if (!this.searchConfig.hasOptList(key)) return;

	op = op || "IN";
	if (op != "IN") return;

	var idkey = "qkey_" + this.nextNo,
	idop = "qop_" + this.nextNo,
	idval = "qval_" + this.nextNo;
	this.nextNo ++;
	var $row = this.addQueryCommon(key, this.viewConfig.headerOf(key), op, idkey, idop, idval);

	var $div = $('<div class="form-control">').appendTo($row);

	var optList = this.searchConfig.optListOf(key);
	var values = value ? value.split(',') : [];
	$.each(optList, function(opt, text) {
		var $lbl = $('<label>').addClass("checkbox-inline").text(text).appendTo($div);
		var $chk = $('<input type="checkbox">')
			.attr('id', idval).val(opt).prependTo($lbl);
		if ($.inArray(opt, values) != -1) $chk.attr('checked', 'checked');
	});
}
Searcher.prototype.addTagsQuery = function(key, op, value) {
	if (!this.searchConfig.hasOptList(key)) return;

	op = op || "TAGS";
	if (op != "TAGS") return;

	var idkey = "qkey_" + this.nextNo,
	idop = "qop_" + this.nextNo,
	idval = "qval_" + this.nextNo;
	this.nextNo ++;
	var $row = this.addQueryCommon(key, this.viewConfig.headerOf(key), op, idkey, idop, idval);

	var $div = $('<div>').css({
		display: "inline-block",
		verticalAlign: "middle",
		padding: "0 10px 4px 10px",
		marginBottom: 10,
		border: "solid 1px #ccc",
		borderRadius: 4,
	}).appendTo($row);

	var optList = this.searchConfig.optListOf(key);
	var values = value ? value.split(',') : [];
	$.each(optList, function(opt, text) {
		var $lbl = $('<label>').addClass("checkbox inline").appendTo($div);
		var $tag = $('<span class="label">').text(text).appendTo($lbl);
		if (text.indexOf('?') != -1) $tag.addClass('label-warning');
		if (text.indexOf('!') != -1) $tag.addClass('label-important');
        var $chk = $('<input type="checkbox" class="form-control">')
			.attr('id', idval).val(opt).prependTo($lbl);
		if ($.inArray(opt, values) != -1) $chk.attr('checked', 'checked');
	});
}
Searcher.prototype.addTextQuery = function(key, op, value) {
	op = op || "STR_MATCH";

	if ($.inArray(op, ["EQ", "STR_MATCH", "STR_BEGIN", "STR_END", "STR_REGEX"]) == -1) return;

	var idkey = "qkey_" + this.nextNo,
	idop = "qop_" + this.nextNo,
	idval = "qval_" + this.nextNo;
	this.nextNo ++;

	var $row = this.addQueryCommon(key, this.viewConfig.headerOf(key), op, idkey, idop, idval);

	var $div = $('<div class="input-group">').appendTo($row);

	var opnames = {"STR_MATCH":"匹配","EQ":"为","STR_BEGIN":"开始为","STR_END":"末尾为","STR_REGEX":"正则"};
	var opmenu = [{op:"STR_MATCH",text:"匹配"}, null,{op:"EQ",text:"为"},{op:"STR_BEGIN",text:"开始为"},{op:"STR_END",text:"末尾为"},null,{op:"STR_REGEX",text:"正则表达式匹配"}];
	this.addQueryOpMenu($div, op, idop, opmenu, opnames);

	$input = $('<input type="text" class="form-control" maxlength="50">')
		.attr('id', idval).val(value).appendTo($div);
}
Searcher.prototype.addNumQuery = function(key, op, value) {
	op = op || "EQ";
	if ($.inArray(op, ["EQ", "LE", "GE"]) == -1) return;

	var idkey = "qkey_" + this.nextNo,
	idop = "qop_" + this.nextNo,
	idval = "qval_" + this.nextNo;
	this.nextNo ++;

	var $row = this.addQueryCommon(key, this.viewConfig.headerOf(key), op, idkey, idop, idval);

    var $div = $('<div class="input-group">').appendTo($row);

	var opnames = {"EQ":"等于","GE":"至少为","LE":"最大为"};
	var opmenu = [{op:"EQ",text:"等于"},{op:"GE",text:"至少为"},{op:"LE",text:"最大为"}];
	this.addQueryOpMenu($div, op, idop, opmenu, opnames);

    $input = $('<input type="number" class="form-control" maxlength="50">')
		.attr('id', idval).val(value).appendTo($div);
}
Searcher.prototype.addDateQuery = function(key, op, value) {
	op = op || "EQ";
	if ($.inArray(op, ["EQ", "LE", "GE"]) == -1) return;

	var idkey = "qkey_" + this.nextNo,
	idop = "qop_" + this.nextNo,
	idval = "qval_" + this.nextNo;
	this.nextNo ++;

	var $row = this.addQueryCommon(key, this.viewConfig.headerOf(key), op, idkey, idop, idval);

    var $div = $('<div class="input-group">').appendTo($row);

	var opnames = {"EQ":"为","GE":"不早于","LE":"不晚于"};
	var opmenu = [{op:"EQ",text:"日期为"},{op:"GE",text:"日期不早于"},{op:"LE",text:"日期不晚于"}];
	this.addQueryOpMenu($div, op, idop, opmenu, opnames);

	$input = $('<input type="date" class="form-control Wdate" maxlength="50">')
		.attr('onchange',"return $(this).valid();")
		//.attr('onfocus', "WdatePicker(datePickerSettings());")
		.attr('id', idval).val(value).appendTo($div);
}
Searcher.prototype.collectQuery = function() {
	var queryList = [];
	for (var i = 0; i < this.nextNo; i++) {
		var idkey = "qkey_" + i, idop = "qop_" + i, idval = "qval_" + i;
		if ($('#' + idval).length == 0) continue;

		var key = $('#' + idkey).val();
		var op = $('#' + idop).val();
		var val = !/IN|TAGS/.test(op) ? $('#' + idval).val() :
			$.map($("[id=" + idval + "]:checked"),
					function(e) {return $(e).val()}).join(',');
		queryList.push({ key: key, op: op, val: val });
	}
	return queryList;
}
