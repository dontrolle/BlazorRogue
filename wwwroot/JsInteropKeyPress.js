// From here:
// https://github.com/XelMed/BlazorSnake/blob/master/BlazorSnake/wwwroot/JsInteropKeyPress.js
window.AddOnKeyDownEvent = () => {
    document.onkeydown = function (evt) {
      evt = evt || window.event;
      DotNet.invokeMethodAsync('BlazorRogue', 'JsKeyDown', evt.keyCode);
  
      //Prevent all but F5 and F12
      if (evt.keyCode !== 116 && evt.keyCode !== 123)
        evt.preventDefault();
    };
  };
  window.AddOnKeyUpEvent = () => {
    document.onkeyup = function (evt) {
      evt = evt || window.event;
    //  alert(evt.keyCode);      
      DotNet.invokeMethodAsync('BlazorRogue', 'JsKeyUp', evt.keyCode);
      //Prevent all but F5 and F12
      if (evt.keyCode !== 116 && evt.keyCode !== 123)
        evt.preventDefault();
    };
  };
  window.MySetFocus = (ctrl) => {
      document.getElementById(ctrl).focus();
      return true;
  };