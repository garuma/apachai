/* Author: Jérémie Laval
   
*/

var img = window.location.href.slice(window.location.href.lastIndexOf('/') + 1);
if (img != "i") {
	var src = "/Content/img/" + img;
	$("#mainImage").attr("src", src);
	$("meta[property=\"og:image\"]").attr ("content", src);
	
	$.get("/infos/" + img, callback = function (data, textStatus, xhr) {
		if (data.length == 0) {
			$("#pictable").append ("<em>Sorry, nothing to see here</em>");
			return;
		}

		$.each (data, function (key, value) {
			$("#pictable").append ("<tr><td class=\"title\">" + key + "</td><td>" + value + "</td></tr>");
		})}, "json");

	$.get("/tweet/" + img, callback = function (data, textStatus, xhr) {
		$("#imgAvatar").attr ("src", data["avatar"]);
		$("#tweetText").html (data["tweet"]);
	}, "json");
}
