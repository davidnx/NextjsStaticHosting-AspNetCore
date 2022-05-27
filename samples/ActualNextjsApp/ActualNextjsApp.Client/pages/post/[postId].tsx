import React from 'react'
import type { NextPage } from 'next'
import Link from 'next/link'
import { useRouter } from 'next/router'

const Home: NextPage = () => {

  const [postId, setPostId] = React.useState<string | undefined>();

  const router = useRouter();
  React.useEffect(() => {
    setPostId(router.query.postId as string | undefined);
  }, [router]);

  return (
    <div className="container">
      <h1>This is post {postId}</h1>

      <h1>Other pages:</h1>
      <ul>
        <li><Link href="/">Home</Link></li>
      </ul>
    </div>
  )
}

export default Home;
