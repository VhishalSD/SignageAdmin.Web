// Both management and the player resolve stored paths relative to the web root.
window.SignageMedia = {
    resolve: function (value) {
        var path = String(value || '').trim();
        if (!path) return '';
        try {
            var url = new URL(path, location.origin + '/');
            return url.protocol === 'http:' || url.protocol === 'https:' ? url.href : '';
        } catch {
            return '';
        }
    }
};
