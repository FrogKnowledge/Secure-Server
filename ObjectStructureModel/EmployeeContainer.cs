using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace CommonTypes
{
    public class EmployeeContainer : INotifyPropertyChanged
    {

        public EmployeeContainer()
        { }

        public EmployeeContainer(int id, string name, string surname, string patronymic, string profession, string birthday, string dateOfEmployment, string adress, byte[] photo, string login, byte accessLevel, string other)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Surname = surname ?? throw new ArgumentNullException(nameof(surname));
            Patronymic = patronymic ?? throw new ArgumentNullException(nameof(patronymic));
            Profession = profession ?? throw new ArgumentNullException(nameof(profession));
            Birthday = birthday ?? throw new ArgumentNullException(nameof(birthday));
            DateOfEmployment = dateOfEmployment ?? throw new ArgumentNullException(nameof(dateOfEmployment));
            Photo = photo ?? throw new ArgumentNullException(nameof(photo));
            Adress = adress ?? throw new ArgumentNullException(nameof(adress));
            Other = other ?? throw new ArgumentNullException(nameof(other));
            Login = login ?? throw new ArgumentNullException(nameof(login));
            AccessLevel = accessLevel;
        }
        public void CopyAllFields(EmployeeContainer container)
        {
            Id = container.Id;
            Name = new String(container.Name);
            Surname = new String(container.Surname);
            Patronymic = new String(container.Patronymic);
            Profession = new String(container.Profession);
            Birthday = new String(container.Birthday);
            DateOfEmployment = new String(container.DateOfEmployment);
            Photo = container.Photo;
            Adress = new String(container.Adress);
            Other = new String(container.Other);
            Login = new String(container.Login);
            Password = new String(container.Password);
            AccessLevel = container.AccessLevel;
        }
        public void Clear()
        {
            Id = 0;
            Name = "";
            Surname = "";
            Patronymic = "";
            Profession = "";
            Birthday = "";
            DateOfEmployment = "";
            Photo = null;
            Adress = "";
            Other = "";
            Password = "";
            Login = "";
            AccessLevel = 0;
        }
        public bool IsValid()
        {
            if (Name.TrimEnd() != "" && Surname.TrimEnd() != "" && Patronymic.TrimEnd() != ""
                && Adress.TrimEnd() != "" && Other.TrimEnd() != "")
            {
                return true;
            }
            return false;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }
        public int id;
        public string name;
        public string surname;
        public string patronymic;
        public string profession;
        public string birthday;
        public string dateOfEmployment;
        public string adress;
        public string other;
        public string login;
        public string password;

        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                OnPropertyChanged();
            }
        }
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        public string Surname
        {
            get
            {
                return surname;
            }
            set
            {
                surname = value;
                OnPropertyChanged();
            }
        }
        public string Patronymic
        {
            get
            {
                return patronymic;
            }
            set
            {
                patronymic = value;
                OnPropertyChanged();
            }
        }
        public string Profession
        {
            get
            {
                return profession;
            }
            set
            {
                profession = value;
                OnPropertyChanged();
            }
        }
        public string Birthday
        {
            get
            {
                return birthday;
            }
            set
            {
                birthday = value;
                OnPropertyChanged();
            }
        }
        public string DateOfEmployment
        {
            get
            {
                return dateOfEmployment;
            }
            set
            {
                dateOfEmployment = value;
                OnPropertyChanged();
            }
        }
        public byte[] Photo { get; set; }
        public string Adress
        {
            get
            {
                return adress;
            }
            set
            {
                adress = value;
                OnPropertyChanged();
            }
        }
        public string Other
        {
            get
            {
                return other;
            }
            set
            {
                other = value;
                OnPropertyChanged();
            }
        }
        public string Login
        {
            get
            {
                return login;
            }
            set
            {
                login = value;
                OnPropertyChanged();
            }
        }
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
                OnPropertyChanged();
            }
        }
        public byte AccessLevel { get; set; }


    }
}
