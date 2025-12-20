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

// Toast notifications
window.showToast = (type, message) => {
    // Check if message contains HTML (for bullet lists)
    const containsHtml = /<[^>]+>/.test(message);
    
    const Toast = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: containsHtml ? 5000 : 3000, // Longer timer for lists
        timerProgressBar: true,
        customClass: {
            popup: containsHtml ? 'toast-with-list' : ''
        },
        didOpen: (toast) => {
            toast.addEventListener('mouseenter', Swal.stopTimer);
            toast.addEventListener('mouseleave', Swal.resumeTimer);
        }
    });

    if (containsHtml) {
        // Use html property for messages containing HTML (like bullet lists)
        Toast.fire({
            icon: type,
            html: message
        });
    } else {
        // Use title for simple text messages
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
