/* Author: Jérémie Laval
   
*/

var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
if ($.inArray ("error=1", hashes) != -1) {
	$("#invalid").removeClass("hidden");
}

function onFileChange (value) {
	if (!value.toLowerCase ().match (".*\.(jpg|jpeg)")) {
		$("#invalid").removeClass ("hidden");
		$("#submit").attr ("disabled", "disabled");
	} else {
		$("#invalid").addClass ("hidden");
		$("#submit").removeAttr ("disabled");
	}
}

function onTwitterChange (value) {
	var left = 100 - value.length;
	var elem = $("#numCharacters");
	elem.text (left);

	if (left < 0) {
		elem.addClass ("numWrong");
		elem.removeClass ("numRight");
		$("#submit").attr ("disabled", "disabled");
	} else {
		elem.addClass ("numRight");
		elem.removeClass ("numWrong");
		$("#submit").removeAttr ("disabled");
	}
}

$('#effect').change (function () {
	var srcs = { "eff_original" : "cat.jpg",
				 "eff_sepia" : "cat_sepia.jpg",
				 "eff_invert" : "cat_invert.jpg",
				 "eff_blackwhite" : "cat_bw.jpg"};
	var value = $('#effect').val();

	if (value in srcs)
		$('#img_preview img').attr('src', "/Content/img/" + srcs[value]);
});