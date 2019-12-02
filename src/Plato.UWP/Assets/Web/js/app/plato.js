
function addCss(path) {

    var head = window.document.head;
    if (head) {
        var link = document.createElement("link");
        link.type = "text/css";
        link.rel = "stylesheet";
        link.href = path;
        head.appendChild(link);
    }

}

function addUrl(url) {

    var el = document.getElementById("url");
    el.href = url;
    el.innerText = url;

}