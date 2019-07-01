window.SetFocus = (div) => {
    document.getElementById(div).focus();
    return true;
}

window.HideById = (id) => {
    document.getElementById(id).style.display = "none";
    return true;
}

window.ShowById = (id) => {
    document.getElementById(id).style.display = "block";
    return true;
}