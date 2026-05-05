// Unity WebGL Loader for standard web (no Telegram)

// Lazy load background image for better performance
function lazyLoadBackground() {
    var img = new Image();
    img.onload = function() {
        document.body.classList.add('bg-loaded');
    };
    img.src = 'TemplateData/BG.png';
}

// Load background after page load
window.addEventListener("load", function () {
    lazyLoadBackground();
});

// Global references
var container = document.querySelector("#unity-container");
var canvas = document.querySelector("#unity-canvas");
var loadingBar = document.querySelector("#unity-loading-bar");
var progressBarFull = document.querySelector("#unity-progress-bar-full");
var warningBanner = document.querySelector("#unity-warning");

// Show banner for warnings/errors
function unityShowBanner(msg, type) {
    function updateBannerVisibility() {
        warningBanner.style.display = warningBanner.children.length ? 'block' : 'none';
    }

    var div = document.createElement('div');
    div.innerHTML = msg;
    warningBanner.appendChild(div);

    if (type === 'error') {
        div.style.cssText = 'background: #ff4444; color: white; padding: 15px; border-radius: 8px; margin: 10px;';
    } else if (type === 'warning') {
        div.style.cssText = 'background: #ffc107; color: #000; padding: 15px; border-radius: 8px; margin: 10px;';
        setTimeout(function () {
            if (div.parentNode) {
                warningBanner.removeChild(div);
                updateBannerVisibility();
            }
        }, 5000);
    }

    updateBannerVisibility();
}


// Unity build configuration
var buildUrl = "Build";
var loaderUrl = buildUrl + "/{{{ LOADER_FILENAME }}}";
var config = {
    dataUrl: buildUrl + "/{{{ DATA_FILENAME }}}",
    frameworkUrl: buildUrl + "/{{{ FRAMEWORK_FILENAME }}}",
    #if USE_THREADS
    workerUrl: buildUrl + "/{{{ WORKER_FILENAME }}}",
    #endif
    #if USE_WASM
    codeUrl: buildUrl + "/{{{ CODE_FILENAME }}}",
    #endif
    #if MEMORY_FILENAME
    memoryUrl: buildUrl + "/{{{ MEMORY_FILENAME }}}",
    #endif
    #if SYMBOLS_FILENAME
    symbolsUrl: buildUrl + "/{{{ SYMBOLS_FILENAME }}}",
    #endif
    streamingAssetsUrl: "StreamingAssets",
    companyName: "{{{ COMPANY_NAME }}}",
    productName: "{{{ PRODUCT_NAME }}}",
    productVersion: "{{{ PRODUCT_VERSION }}}",
    showBanner: unityShowBanner,
};

// Mobile device viewport setup
if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
    var meta = document.createElement('meta');
    meta.name = 'viewport';
    meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';
    document.getElementsByTagName('head')[0].appendChild(meta);
}

#if BACKGROUND_FILENAME
canvas.style.background = "url('" + buildUrl + "/{{{ BACKGROUND_FILENAME.replace(/'/g, '%27') }}}') center / cover";
#endif

loadingBar.style.display = "block";

// Load Unity
var script = document.createElement("script");
script.src = loaderUrl;
script.onload = function () {
    createUnityInstance(canvas, config, function (progress) {
        progressBarFull.style.width = 100 * progress + "%";
    }).then(function (unityInstance) {
        loadingBar.style.display = "none";
    }).catch(function (message) {
        alert(message);
    });
};
document.body.appendChild(script);
