// SweetAlert2 confirmation dialog
window.confirmDelete = (title, message) => {
    return new Promise((resolve) => {
        Swal.fire({
            title: title,
            text: message,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6',
            confirmButtonText: 'Yes, delete it!',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            resolve(result.isConfirmed);
        });
    });
};

// Toast notifications with HTML support for bullet lists
window.showToast = (type, message) => {
    // Check if message contains HTML (bullet list)
    const isHtmlContent = message && (message.includes('<ul') || message.includes('<li') || message.includes('<ol'));
    
    // Use longer timer for HTML lists (more content to read)
    const timerDuration = isHtmlContent ? 8000 : 3000;
    
    const Toast = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: timerDuration,
        timerProgressBar: true,
        didOpen: (toast) => {
            toast.addEventListener('mouseenter', Swal.stopTimer);
            toast.addEventListener('mouseleave', Swal.resumeTimer);
        },
        customClass: isHtmlContent ? { popup: 'toast-with-list' } : {}
    });

    // Use 'html' property for HTML content, 'title' for plain text
    if (isHtmlContent) {
        Toast.fire({
            icon: type,
            html: message
        });
    } else {
        Toast.fire({
            icon: type,
            title: message
        });
    }
};

// Show loading indicator
window.showLoading = (message) => {
    Swal.fire({
        title: message || 'Loading...',
        allowOutsideClick: false,
        allowEscapeKey: false,
        allowEnterKey: false,
        showConfirmButton: false,
        willOpen: () => {
            Swal.showLoading();
        }
    });
};

// Hide loading indicator
window.hideLoading = () => {
    Swal.close();
};

// Confirm dialog for generic confirmations
window.confirmAction = (title, message, confirmButtonText = 'Confirm') => {
    return new Promise((resolve) => {
        Swal.fire({
            title: title,
            text: message,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#6c757d',
            confirmButtonText: confirmButtonText,
            cancelButtonText: 'Cancel'
        }).then((result) => {
            resolve(result.isConfirmed);
        });
    });
};

// Show delete confirmation dialog (alias for Department page compatibility)
window.showDeleteConfirmation = (title, message) => {
    return new Promise((resolve) => {
        Swal.fire({
            title: title,
            text: message,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, delete it!',
            cancelButtonText: 'Cancel',
            reverseButtons: true
        }).then((result) => {
            resolve(result.isConfirmed);
        });
    });
};
