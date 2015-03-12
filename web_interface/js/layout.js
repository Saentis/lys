/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/
function Layout(vm, exampleApps)
{
	var self = this;
	var currentApp = null;
	var apps = [];
	var apps_onlylocal = [];
	var MAX_WIDTH = 30;
	var MAX_HEIGHT = 20;
	var isFullScreen = false;
	var fullScreenZoom = 1;
	var isInFullScreenAnimation = false;

	var appList = $('<div id="applist"/>').
		append('<div id="logo"/>').
		append($("<p/>").text("Applications in local storage")).
		append($("<ul/>")).
		append($("<p/>").text("Example applications")).
		append($("<ul/>"));
	var sidenav = $('<div id="sidenav"/>').css("width", "400px").
		append('<div id="logo"/>').
		append($("<div/>").
			css({ margin: "10px 25px -30px 0", textAlign: "right" }).
			append($("<a/>").attr("href", "javascript:layout.fullscreen()").text("Toggle full screen"))).
		append('<div id="ledboard"/>').
		append($("<p/>").
			append($("<button/>").addClass("green fixedwidth").text("Run").click(function() { self.run(); })).
			append(" ").
			append($("<button/>").addClass("red fixedwidth").text("Stop execution").click(function() { self.stop(); })));
	var content = $('<div id="content"/>').
		append($("<p/>").
			append($("<button/>").addClass("blue").text("Save application").click(function() { self.save(this, true); })).
			append(" ").
			append($("<button/>").addClass("blue").text("Save application and return").click(function() { self.saveandreturn(this); })).
			append(" ").
			append($("<button/>").addClass("red").text("Discard changes and return").click(function() { self.discard(); }))).
		append($('<p id="noteProtectedApp"/>').text("This is an example application, which cannot be modified. You can save it as a local copy, though.")).
		append($("<h2/>").text("Application settings")).
		append($("<p/>").
			append($('<label for="fldDimW"/>').text("Dimension (width × height)")).
			append($('<input type="text" id="fldDimW"/>').addClass("field small")).
			append(" × ").
			append($('<input type="text" id="fldDimH"/>').addClass("field small")).
			append(" LED's")).
		append($("<p/>").
			append($('<label for="fldAppName"/>').text("Application name")).
			append($('<input type="text" id="fldAppName"/>').addClass("field"))).
		append($("<p/>").
			append($('<label for="fldEntryPoint"/>').text("Entry point")).
			append($('<input type="text" id="fldEntryPoint"/>').addClass("field"))).
		append($("<h2/>").text("LYS source code")).
		append($('<textarea id="sourcecode"/>').addClass("field")).
		append($("<h2/>").text("Debug log")).
		append($('<div id="runtimeErrors"/>'));

	$(vm).
		on("runtimeError", function (e, error) { debugLog(error).addClass("error"); }).
		on("runtimeLog", function (e, msg) { debugLog(msg); });

	function debugLog(msg)
	{
		msg = "" + msg;
		var o = $("<div/>").append($("<small/>").text(timeString())).append($("<div/>").text(msg));
		var tmp;
		if ((tmp = msg.indexOf("error at ")) >= 0)
		{
			var pos = parseInt(msg.substring(tmp + 9));
			o.addClass("clickable").click(function()
			{
				$("html, body").animate({ scrollTop: $("#sourcecode").offset().top - 100 }, 500);
				$("#sourcecode").get(0).selectionStart = pos;
				$("#sourcecode").get(0).selectionEnd = pos;
				$("#sourcecode").focus();
			});
			o.find(">div").append("<br/><small>Click here to jump to position in source code.</small>");
		}
		$("#runtimeErrors").append(o);
		return o;
	}
	function timeString()
	{
		var date = new Date();
		var h = date.getHours();
		var m = date.getMinutes();
		var s = date.getSeconds();
		var u = date.getMilliseconds();
		return (h<10?"0"+h:h) + ":" + (m<10?"0"+m:m) + ":" + (s<10?"0"+s:s) + "." + (u<10?"00"+u:(u<100?"0"+u:u));
	}
	function uiAddApp(selector, app)
	{
		var result;
		if (app)
			result = $("<li/>").text(app.app.name).click(function() { self.showApp(app); });
		else
			result = $("<li/>").append($("<em/>").text("Create new application")).click(function() { self.showApp(null); });
		if (app && !app.isProtected)
		{
			result.append($("<button/>").addClass("red small").text("delete").click(function(e)
			{
				e.stopPropagation();
				if (confirm("Do you really want to delete this application? This cannot be undone."))
					deleteApp(app);
			}));
		}
		selector.append(result);
		return result;
	}
	function showError(selector)
	{
		$("html, body").animate({ scrollTop: selector.offset().top - 100 }, 500, function()
		{
			for (var i = 0; i < 4; i++)
				selector.transition({ opacity: (i%2), duration: 60 }, 'snap');
		});
	}
	function checkErrors()
	{
		var erroneousFields = $();
		var w = $("#fldDimW").val() * 1 || 0;
		var h = $("#fldDimH").val() * 1 || 0;
		if (w <= 0 || w > MAX_WIDTH) erroneousFields = erroneousFields.add("#fldDimW");
		if (h <= 0 || h > MAX_HEIGHT) erroneousFields = erroneousFields.add("#fldDimH");
		if (!$("#fldAppName").val()) erroneousFields = erroneousFields.add("#fldAppName");
		if (!$("#fldEntryPoint").val()) erroneousFields = erroneousFields.add("#fldEntryPoint");
		if (erroneousFields.length > 0)
		{
			showError(erroneousFields);
			return false;
		}
		return true;
	}
	function showVMStatusText(container, text)
	{
		container.empty().append($("<span/>").css({ display: "block", textAlign: "left", color: "#fff" }).text(text));
	}
	function showVMLoadingAnimation(container)
	{
		var box = $("<span/>").css({ display: "block", textAlign: "left", color: "#fff" }).append($("<span/>").addClass("statusText").text("Compiling ...")).append("<br/>");
		container.append(box);
		var o = [];
		for (var i = 0; i < 3; i++)
			box.append(o[i] = $("<span/>").css({ display: "inline-block", width: "8px", height: "8px", borderRadius: "4px", marginRight: "3px", backgroundColor: "#fff" }));
		var loop = function()
		{
			for (var i = 0; i < o.length; i++)
				o[i].delay(i * 150).animate({ marginLeft: 20 }).animate({ marginLeft: 0 });
			o[0].delay(600).queue(function()
			{
				$(this).dequeue();
				loop();
			});
		};
		loop();
	}
	function deleteApp(app)
	{
		if (!app || app.isProtected) return;

		// Remove it from the list of all app containers
		var tmp = [];
		for (var i = 0; i < apps.length; i++)
			if (apps[i] != app)
				tmp.push(apps[i]);
		apps = tmp;
		// Remove it from the list of local apps.
		// Note that the latter only contains the app, not its container!
		var tmp = [];
		for (var i = 0; i < apps_onlylocal.length; i++)
			if (apps_onlylocal[i] != app.app)
				tmp.push(apps_onlylocal[i]);
		apps_onlylocal = tmp;
		// Save
		localStorage.apps = JSON.stringify(apps_onlylocal);
		// Remove from UI
		app.ui.remove();
	}

	window.onbeforeunload = function()
	{
		if (currentApp && currentApp.isModified)
			return "Are you sure to discard your changes?";
	};

	this.load = function()
	{
		uiAddApp(appList.find("ul:first"), null);
		var localApps = typeof Storage !== "undefined" ? (localStorage.apps ? ($.parseJSON(localStorage.apps) || []) : []) : [];
		for (var i = 0; i < localApps.length; i++)
		{
			var appContainer = { app: localApps[i], isNew: false, isProtected: false, isModified: false };
			appContainer.ui = uiAddApp(appList.find("ul:first"), appContainer);
			apps.push(appContainer);
			apps_onlylocal.push(localApps[i]);
		}
		for (var i = 0; i < exampleApps.length; i++)
		{
			var appContainer = { app: exampleApps[i], isNew: false, isProtected: true, isModified: false };
			appContainer.ui = uiAddApp(appList.find("ul:last"), appContainer);
			apps.push(appContainer);
		}

		$("#loading").after(appList).after(sidenav.hide()).after(content.hide()).remove();
		$("#fldDimW,#fldDimH").change(function()
		{
			var value = $(this).val() * 1;
			var isW = this.id == "fldDimW";
			var maxValue = isW ? MAX_WIDTH : MAX_HEIGHT;
			var appW = isW ? value : currentApp.app.w;
			var appH = !isW ? value : currentApp.app.h;
			if (value > 0 && value <= maxValue)
			{
				$(this).removeClass("error");
				if (isW)
				{
					var sideNavWidth = Math.max(400, value * 22 + 70);
					$("#sidenav").transition({ width: sideNavWidth });
					$("#ledboard").transition({ width: value * 22 });
					$("#content").transition({ marginRight: sideNavWidth + 10 });
				}
				else
				{
					$("#ledboard").transition({ height: value * 22 });
				}
			}
			else
				$(this).addClass("error");
		});
		$("#fldAppName,#fldEntryPoint").change(function()
		{
			if ($(this).val())
				$(this).removeClass("error");
			else
				$(this).addClass("error");
		});
		//$("#sourcecode").get(0).contentEditable = true;
		$("#sourcecode").keydown(function(e)
		{
			if ((e.keyCode || e.which) == 9)
			{
				e.preventDefault();
				var start = this.selectionStart;
				var end = this.selectionEnd;
				var value = $(this).val();
				$(this).val(value.substring(0, start) + "\t" + value.substring(end));
				this.selectionStart = this.selectionEnd = start + 1;
			}
		});
	};
	this.showApp = function(app)
	{
		if (!app)
		{
			app = { name: "New application", w: 10, h: 5, entry: "myApp::main", code: "namespace myApp\n{\n\tvoid main()\n\t{\n\t}\n}" };
			app = { app: app, isNew: true, isProtected: false, isModified: false };
		}
		currentApp = app;

		var sideNavWidth = Math.max(400, app.app.w * 22 + 70);
		$("#sidenav").css({ width: sideNavWidth });
		$("#ledboard").css({ width: app.app.w * 22, height: app.app.h * 22 });
		$("#content").css({ marginRight: sideNavWidth + 10 });
		$("#fldDimW").val(app.app.w);
		$("#fldDimH").val(app.app.h);
		$("#fldAppName").val(app.app.name);
		$("#fldEntryPoint").val(app.app.entry);
		$("#sourcecode").val(app.app.code);
		$("#sidenav button.green").removeAttr("disabled");
		$("#sidenav button.red").attr("disabled", "disabled");
		if (app.isProtected)
			$("#noteProtectedApp").show();
		else
			$("#noteProtectedApp").hide();
		$("#ledboard").empty();
		$("#runtimeErrors").empty();

		appList.transition({ scale: 1.2, opacity: 0, complete: function()
		{
			$(this).hide();
			sidenav.add(content).css({ scale: 1.2, opacity: 0 }).show().transition({ scale: 1, opacity: 1});
		}});
	};
	this.save = function(saveButton, showSaveNote)
	{
		if (!currentApp) return;
		if (!checkErrors()) return;
		
		// Create a copy of protected apps
		if (currentApp.isProtected)
		{
			currentApp = $.extend(true /* deep copy */, {}, currentApp);
			currentApp.isProtected = false;
			currentApp.isNew = true;
		}

		currentApp.app.w = $("#fldDimW").val() * 1 || 0;
		currentApp.app.h = $("#fldDimH").val() * 1 || 0;
		currentApp.app.name = $("#fldAppName").val();
		currentApp.app.entry = $("#fldEntryPoint").val();
		currentApp.app.code = $("#sourcecode").val();

		if (currentApp.isNew)
		{
			currentApp.isNew = false;
			currentApp.ui = uiAddApp($("#applist>ul:first"), currentApp);
			apps.push(currentApp);
			apps_onlylocal.push(currentApp.app);
		}
		else
		{
			currentApp.ui.text(currentApp.app.name);
		}

		if (typeof Storage !== "undefined")
		{
			localStorage.apps = JSON.stringify(apps_onlylocal);
			currentApp.isModified = false;
			if (showSaveNote)
			{
				var w = $(saveButton).width();
				var note = $("<span/>").css({ display: "inline-block", width: w, lineHeight: "40px", textAlign: "center", position: "absolute" }).text("Saved.");
				$(saveButton).before(note);
				$(saveButton).transition({ scale: 1.5, opacity: 0, duration: 300 }).transition({ scale: 1, opacity: 1, duration: 300, delay: 1000, complete: function() { note.remove(); } });
			}
			return true;
		}
		else
		{
			alert("Sorry, your browser does not support local storage. Applications cannot be saved.");
			return false;
		}
	};
	this.discard = function()
	{
		self.stop();
		if (!currentApp || (currentApp.isModified && !confirm("If you proceed, the changes you made will be discarded.")))
			return;
		currentApp.isModified = false;
		currentApp = null;

		sidenav.add(content).transition({ scale: 1.2, opacity: 0, complete: function()
		{
			$(this).hide();
			appList.css({ scale: 1.2, opacity: 0 }).show().transition({ scale: 1, opacity: 1});
		}});
	};
	this.saveandreturn = function(saveButton)
	{
		if (self.save(saveButton, false))
			self.discard();
	};
	this.run = function()
	{
		$("#ledboard").empty();
		$("#runtimeErrors").empty();
		if (!checkErrors()) return;

		debugLog("Compiling application ...").addClass("info");

		$(".field").attr("disabled", "disabled");
		$("#sidenav button").attr("disabled", "disabled");
		showVMLoadingAnimation($("#ledboard"));

		var appName = $("#fldAppName").val();
		var code = $("#sourcecode").val();

		$.ajax("compile.php", {
			data: { source: code },
			type: "POST",
			error: function (sender, status, e)
			{
				showVMStatusText($("#ledboard"), "Compiler not available.");
				debugLog("Compiler not available: " + status).addClass("error");
				$(".field,#sidenav .green").removeAttr("disabled");
			},
			success: function (resp)
			{
				$(".field").not("#fldDimW,#fldDimH").removeAttr("disabled");
				if (resp.status)
				{
					showVMStatusText($("#ledboard"), "Compilation failed.");
					debugLog(resp.str).addClass("error");
					$("#sidenav .green").removeAttr("disabled");
					return;
				}
				else
				{
					$("#ledboard .statusText").text("Loading ...");
					debugLog("Compilation succeeded.").addClass("success");
					debugLog("Fetching application ...").addClass("info");
					$.getScript("compile-result.php?callback=layout.runApp&token=" + resp.token + "&iv=" + resp.iv).
						fail(function(jqxhr, settings, exception)
						{
							if (jqxhr.status != 200)
							{
								showVMStatusText($("#ledboard"), jqxhr.responseText);
								debugLog("Unable to load application (HTTP " + jqxhr.status + "): " + jqxhr.responseText).addClass("error");
							}
							else
							{
								showVMStatusText($("#ledboard"), exception);
								debugLog("Unable to load application (Javascript exception): " + exception).addClass("error");
							}
							$("#sidenav .green").removeAttr("disabled");
							return;
						});
				}
			}
		});
	};
	this.runApp = function(app)
	{
		var size = ($("#fldDimW").val() * 1 || 0) * ($("#fldDimH").val() * 1 || 0);
		var entryPoint = $("#fldEntryPoint").val();

		$("#ledboard").empty();
		for (var i = 0; i < size; i++)
			$("#ledboard").append('<div id="led' + i + '"/>');
		$("#ledboard>div").css({ width: 20 * fullScreenZoom, height: 20 * fullScreenZoom - 1 });
		$("#sidenav .red").removeAttr("disabled");

		vm.reset();
		vm.load(app);

		debugLog("Loaded application.").addClass("success");
		debugLog("Application is running.").addClass("info");
		vm.run.apply(vm, (entryPoint + "#0").split("::"));
	};
	this.stop = function()
	{
		debugLog("Application was stopped.").addClass("info");
		vm.stop();
		$(".field").removeAttr("disabled");
		$("#sidenav .green").removeAttr("disabled");
		$("#sidenav .red").attr("disabled", "disabled");
	};
	this.fullscreen = function()
	{
		if (isInFullScreenAnimation) return;
		isInFullScreenAnimation = true;
		isFullScreen = !isFullScreen;
		var appW = $("#fldDimW").val() * 1 || 0;
		var appH = $("#fldDimH").val() * 1 || 0;
		if (isFullScreen)
		{
			// Determine zoom
			var zoomW = ((window.innerWidth - 320 - 40) / appW - 2) / 20;
			var zoomH = ((window.innerHeight - 60 - 47) / appH - 2) / 20;
			var zoom = Math.min(zoomW, zoomH);
			fullScreenZoom = zoom;

			$("#logo").animate({ opacity: 0 }).slideUp();
			$("#sidenav").animate({ left: 0, width: "100%" });
			$("html, body").animate({ scrollTop: 0 });
			$("#ledboard+p>button").css({ marginBottom: 10 });
			$("#ledboard+p").animate({ margin: 0 }, function()
			{
				$(this).css({ position: "absolute", width: "100%", right: 0, top: $(this).offset().top }).
					animate({ width: "150px" }). // button is 120px, we add +30 for margin
					animate({ top: 50 }, function() { isInFullScreenAnimation = false; });
			});
			$("#ledboard>div").fadeOut();
			$("#ledboard").delay(800).animate({ width: appW * (20 * zoom + 2), height: appH * (20 * zoom + 2)}, function()
			{
				$("#ledboard>div").css({ width: 20 * zoom, height: 20 * zoom - 1 }).fadeIn();
			});
		}
		else
		{
			fullScreenZoom = 1;

			$("#ledboard>div").hide(); // no time to animate
			$("#ledboard").animate({ width: appW * 22, height: appH * 22 }, function()
			{
				$("#ledboard>div").css({ width: 20, height: 19 }).fadeIn();
			});
			$("#ledboard+p").fadeOut(function()
			{
				$(this).css({ margin: "20px 0", position: "static", width: "auto" }).find(">button").css({ marginBottom: 0 });
				$(this).fadeIn();
			});
			var sideNavWidth = Math.max(400, appW * 22 + 70);
			$("#sidenav").css({ left: "auto", right: 0 }).delay(400).animate({ width: sideNavWidth});
			$("#logo").delay(400).slideDown().animate({ opacity: 1 }, function() { isInFullScreenAnimation = false; });
		}
	};
}