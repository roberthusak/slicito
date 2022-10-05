let tryFindLinkEventTarget = function(e) {
    let path = e.composedPath();

    for (let i = 0; i < path.length; i++) {
        const element = path[i];
        
        if (element.tagName == 'a') {
            return element;
        }
    }

    return null;
};

let extractLinkHref = function(a) {
    if (a.href instanceof String) {
        return a.href;
    } else {
        return a.href.baseVal;  // SVGAnimatedString
    }
}

document.onclick = function(e) {
    e = e ||  window.event;

    let a = tryFindLinkEventTarget(e);
    if (!a) {
        return true;
    }

    var href = extractLinkHref(a);

    var url = new URL(href);
    if (url.pathname == '/open') {
        // Send the request to open a document in an IDE in the background
        let xhttp = new XMLHttpRequest();
        xhttp.open("GET", href, true);
        xhttp.send();

        // Prevent the redirection of the whole page
        return false;
    }
};