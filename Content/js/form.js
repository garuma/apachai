/* Author: Jérémie Laval
   
*/

var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
if ($.inArray ("error=1", hashes) != -1) {
	$("#invalid").removeClass("hidden");
}


function onFileChange (value) {
	if (!value.match (".*\.(jpg|jpeg|gif|png)")) {
		$("#invalid").removeClass ("hidden");
		$("#submit").attr ("disabled", true);
	} else {
		$("#invalid").addClass ("hidden");
		$("#submit").attr ("disabled", false);
	}
}





















