$(function() {
   function rotateScreenshots() {
      $("#screenshot").animate(
         {backgroundPosition: "-=240px 0px"},
         700,
         'swing',
         function() { 
            setTimeout(rotateScreenshots, 1500);
         }
      );
   }
    
   setTimeout(rotateScreenshots, 1500);
});