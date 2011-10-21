$(function() {
   function rotateScreenshots() {
      $("#screenshot").animate(
         {backgroundPosition: "-=240px 0px"},
         500,
         'swing',
         function() { 
            setTimeout(rotateScreenshots, 3000);
         }
      );
   }
   if (!$.browser.msie || parseInt($.browser.version, 10) >= 9) {
      setTimeout(rotateScreenshots, 3000);
   }
});