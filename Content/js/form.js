/* Author: Jérémie Laval
   
*/

var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
if ($.inArray ("error=1", hashes) != -1) {
	$("#invalid").removeClass("hidden");
}

function onFileChange (value) {
	if (!value.match (".*\.(jpg|jpeg|gif|png)")) {
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