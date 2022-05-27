import React from 'react'
import type { NextPage } from 'next'
import Link from 'next/link'

const Home: NextPage = () => {
  const [counter, setCounter] = React.useState(0);
  React.useEffect(() => {
    const c = setInterval(() => {
      setCounter(counter => counter + 1);
    }, 500);
    return () => clearInterval(c);
  }, []);

  return (
    <div className="container">
      <h1>Home</h1>

      This is a fully-functional Next.js app, check out this counter: {counter}

      <h1>Other pages:</h1>
      <ul>
        <li><Link href="/post/1">Post 1</Link></li>
        <li><Link href="/post/2">Post 2</Link></li>
      </ul>
    </div>
  )
}

export default Home;
