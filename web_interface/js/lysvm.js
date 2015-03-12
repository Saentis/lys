/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/
function LysVM()
{
	var $queue = {};
	var $user = {};
	var matrix = {};
	var $builtin = {
		sys: {
			math: {
				"sin#0": Math.sin,
				"cos#0": Math.cos,
				"tan#0": Math.tan,
				"asin#0": Math.asin,
				"acos#0": Math.acos,
				"atan#0": Math.atan,
				"exp#0": Math.log,
				"sqrt#0": Math.sqrt,
				"ceil#0": Math.ceil,
				"floor#0": Math.floor,
				"pow#0": Math.pow,
				"abs#0": Math.abs,
				"min#0": Math.min,
				"max#0": Math.max,
				"atan2#0": Math.atan2,
				"random#0": Math.random
			},
			log: {
				"info#0": function(msg) { $(self).trigger("runtimeLog", [msg]); },
				"error#0": function(msg) { $(self).trigger("runtimeError", [msg]); },
			}
		},
		"rgb#0": function(led, r, g, b)
		{
			led.beginUpdate();
			led.prop_r(r);
			led.prop_g(g);
			led.prop_b(b);
			led.endUpdate();
		},
		"int#0": function (i) { return Math.floor(i); }, // float to int
		"int#1": function (i) { return parseInt(i); }, // string to int
		"string#0": function (f) { return ""+f; }, // float to string
		"string#1": function (b) { return ""+b; }, // bool to string
		"string#2": function (vec) // vec? to string
		{
			var s = "<";
			for (var i = 0; i < vec.length; i++)
			{
				if (i > 0) s += ", ";
				s += vec[i];
			}
			return s + ">";
		},
	};
	var self = this;

	this.Vector =
	{
		plus: function(vec1, vec2)
		{
			var r = [];
			for (var i = 0; i < vec1.length; i++)
				r.push(vec1[i] + vec2[i]);
			return r;
		},
		minus: function(vec1, vec2)
		{
			var r = [];
			for (var i = 0; i < vec1.length; i++)
				r.push(vec1[i] - vec2[i]);
			return r;
		},
		scalarProduct: function(vec1, vec2)
		{
			var r = 0;
			for (var i = 0; i < vec1.length; i++)
				r += vec1[i] * vec2[i];
			return r;
		},
		times: function(vec, scal)
		{
			var r = [];
			for (var i = 0; i < vec.length; i++)
				r.push(vec[i] * scal);
			return r;
		},
		divide: function(vec, scal)
		{
			var r = [];
			for (var i = 0; i < vec.length; i++)
				r.push(vec[i] / scal);
			return r;
		},
		negate: function(vec)
		{
			var r = [];
			for (var i = 0; i < vec.length; i++)
				r.push(-vec[i]);
			return r;
		},
		equal: function(vec1, vec2)
		{
			for (var i = 0; i < vec1.length; i++)
				if (vec1[i] != vec2[i])
					return false;
			return true;
		},
		notEqual: function(vec1, vec2)
		{
			for (var i = 0; i < vec1.length; i++)
				if (vec1[i] != vec2[i])
					return true;
			return false;
		}
	};

	function $handleQueue(key)
	{
		if (!(key in $queue)) return;
		if ($queue[key].list.length == 0) return;
		if ($queue[key].locked) return;
		var o = $queue[key].list.shift();
		switch (o.type)
		{
			case "f":
				$queue[key].locked = true;
				try
				{
					o.func();
				}
				catch (err)
				{
					$(self).trigger("runtimeError", [err]);
				}
				$queue[key].locked = false;
				$handleQueue(key);
				break;
			case "w":
				$queue[key].locked = true;
				setTimeout(function()
				{
					if (!(key in $queue)) return; // this happens if the VM is stopped
					$queue[key].locked = false;
					$handleQueue(key);
				}, o.time * 1000);
				break;
		}
	}
	function $findFunction(collection, functionPath)
	{
		var o = collection;
		var pathString = "";
		for (var i = 0; i < functionPath.length; i++)
		{
			if (typeof o == "function")
				throw "Namespace '" + pathString + "' is actually a function";
			pathString += (pathString.length == 0 ? "" : "::") + functionPath[i];
			if (functionPath[i] in o)
				o = o[functionPath[i]];
			else
				throw "Could not find namespace/function '" + pathString + "'";
		}
		if (typeof o != "function")
			throw "Could not find function '" + pathString + "'";
		return o;
	}

	this.led = function(ids)
	{
		var sel = $();
		if (typeof ids == "number")
		{
			ids = [ids];
			sel = $("#led" + ids);
		}
		else if (ids instanceof Array)
		{
			for (var i = 0; i < ids.length; i++)
			{
				if (typeof ids[i] != "number")
					throw "Argument of $led() must be an array consisting of integers.";
				sel = sel.add("#led" + ids[i]);
			}
		}
		else
			throw "Argument of $led() must be an integer or an array of integers.";
		return new (function(sel)
		{
			this.toString = function() { return "{LED "+ids+"}"; };
			var r = 0, g = 0, b = 0; // 0.0 to 1.0
			if (ids[0] in $matrix)
			{
				r = $matrix[ids[0]][0];
				g = $matrix[ids[0]][1];
				b = $matrix[ids[0]][2];
			}
			var locked = false;
			function updateCol()
			{
				for (var i = 0; i < ids.length; i++)
					$matrix[ids[i]] = [r, g, b];
				if (!locked)
					sel.css("background-color", "rgb(" + Math.round(r * 0xFF) + "," + Math.round(g * 0xFF) + "," + Math.round(b * 0xFF) + ")");
			}
			this.beginUpdate = function() { locked = true; };
			this.endUpdate = function() { locked = false; updateCol(); };
			this.prop_r = function(val)
			{
				if (typeof val == "undefined")
					return r;
				if (val < 0) val = 0;
				if (val > 1) val = 1;
				r = val; updateCol();
			};
			this.prop_g = function(val)
			{
				if (typeof val == "undefined")
					return g;
				if (val < 0) val = 0;
				if (val > 1) val = 1;
				g = val; updateCol();
			};
			this.prop_b = function(val)
			{
				if (typeof val == "undefined")
					return b;
				if (val < 0) val = 0;
				if (val > 1) val = 1;
				b = val; updateCol();
			};
		})(sel);
	}
	this.async = function(key, func_or_wait)
	{
		if (!(key in $queue))
			$queue[key] = { locked: false, list: [] };
		if (typeof func_or_wait == "function")
			$queue[key].list.push({ type: "f", func: func_or_wait });
		else if (typeof func_or_wait == "number")
			$queue[key].list.push({ type: "w", time: func_or_wait });
		else
			throw "Expected function or number";
		$handleQueue(key);
	}
	this.user = function() // arbitrarily many arguments = path of function
	{
		return $findFunction($user, arguments);
	};
	this.builtin = function() // arbitrarily many arguments = path of function
	{
		return $findFunction($builtin, arguments);
	};
	this.copy = function(obj)
	{
		return $.extend({}, obj);
	};

	this.reset = function()
	{
		$user = [];
		$queue = [];
		$matrix = {};
	};
	this.load = function(ns)
	{
		$.extend($user, ns);
	};
	this.run = function() // arbitrarily many arguments = path of function
	{
		$("#ledboard>div").css({"opacity": 1});
		try
		{
			this.user.apply(this, arguments)();
		}
		catch (err)
		{
			$(self).trigger("runtimeError", [err]);
		}
	};
	this.stop = function()
	{
		$queue = {};
		$("#ledboard>div").animate({"opacity": 0});
	};
}