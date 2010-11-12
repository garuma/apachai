/* Author: Jérémie Laval
   
*/

//$document.ready(function() {
	var img = window.location.href.slice(window.location.href.lastIndexOf('/') + 1);
	if (img != "i") {
		$("#mainImage").attr("src", "/Content/img/" + img);
		
		$.get("/infos/" + img, callback = function (data, textStatus, xhr) {
			$.each (data, function (key, value) {
				$("#pictable").append ("<tr><td class=\"title\">" + key + "</td><td>" + value + "</td></tr>");
			})}, "json");
	}
//});






















