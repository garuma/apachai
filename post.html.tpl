{{body}}
	  <div id="maincolumn">
		<form action="DoPost" method="post" enctype="multipart/form-data">
		  <ul>
			<li>
			  <label for="imagefile">Image file (.jpg):</label>
			  <input type="file" id="imagefile" name="imagefile" accept="image/jpeg" onchange="onFileChange(this.value)" required>
			  <p id="invalid" class="hidden">You haven't entered a valid image file</p>
			</li>
			<li>
			  <div id="img_preview"><img src="/Content/img/cat.jpg" /></div>
			  <div id="preview_select">
				<label for="effect">Choose a fancy effect:</label>
				<select id="effect" name="effect">
				  <option value="eff_original" selected="selected">Original</option>
				  <option value="eff_sepia">Sepia</option>
				  <option value="eff_invert">Invert</option>
				  <option value="eff_blackwhite">Black & White</option>
				</select>
			  </div>
			</li>
			<li>
			  <textarea id="twittertext" name="twittertext" placeholder="Write tweet here, picture URL will be appended to it. Leave blank for no tweet" onKeyUp="onTwitterChange(this.value)" style=""></textarea><br />
			  <div id="numCharacters" class=".numRight">100</div>
			</li>
			<li>
			  <input type="submit" name="submit" id="submit" value="Send" />
			</li>
		  </ul>
		</form>
	  </div>
{{body}}
{{extrascript}}
  <script src="/Content/js/form.js?v=1"></script>
{{extrascript}}
