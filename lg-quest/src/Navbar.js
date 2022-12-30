const Navbar = () => {
    return (
        <nav className="navbar">
            <h1>LG Questionaire</h1>
            <a href="/">Home</a>
            <a href="/create">New Question</a>
        </nav>
    );
}

export default Navbar;
<nav className="navbar navbar-expand-lg navbar-light bg-light">
    <a className="navbar-brand" href="/">LG Questionaire</a>
    <button className="navbar-toggler" type="button" data-toggle="collapse" 
    data-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
        <span className="navbar-toggler-icon"></span>
    </button>
    <div className="collapse navbar-collapse" id="navbarSupportedContent">
    <ul class="navbar-nav mr-auto">
        <li><a href="/">Home</a></li>
        </ul>
    </div>
</nav>