using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Algae.DAL.Models
{
    public class SearchPath : INotifyPropertyChanged
    {
        private bool _isValid;

        public int SearchPathId { get; set; }
        public string Path { get; set; } = string.Empty;
        
        // Properties for UI display
        public bool IsValid 
        { 
            get => _isValid;
            set
            {
                _isValid = value;
                OnPropertyChanged(nameof(IsValid));
            }
        }
        
        public int GamesFound { get; set; } = 0;
        public DateTime? LastScanned { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
